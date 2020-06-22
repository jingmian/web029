namespace ZoomLaCMS.PayOnline
{
    using System;
    using System.Data;
    using System.Configuration;
    using System.Collections;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using ZoomLa.BLL;
    using ZoomLa.Common;
    using ZoomLa.Model;
    using System.Net;
    using ZoomLa.Components;
    using ZoomLa.BLL.Shop;
    using System.Collections.Generic;
    using ZoomLa.SQLDAL;

    /*
     * 币种与平台支付均在该页面指定
     * 现金支付的中转页,支付以其金额为准,支持传入订单号(多张订单以,切割)生成支付单或支付单号补完信息
     */
    public partial class Orderpay : System.Web.UI.Page
    {
        B_PayPlat platBll = new B_PayPlat();
        B_Payment payBll = new B_Payment();
        B_User buser = new B_User();
        B_OrderList orderBll = new B_OrderList();
        B_CartPro bcart = new B_CartPro();//加入购物车
        OrderCommon orderCom = new OrderCommon();
        B_MoneyManage moneyBll = new B_MoneyManage();
        double price = 0, fare = 0, arrive = 0, allamount = 0, point = 0, point_arrive = 0;

        //支持以,号分隔,在该页生成支付单
        public string OrderNo { get { return Request.QueryString["OrderCode"]; } }
        //支付单号码
        public int StoreID { get { return DataConvert.CLng(Request["StoreID"],1); } }
        //根据需要,可在自己页面生成,或传入OrderNo在本页生成
        public string PayNo { get { return Request.QueryString["PayNo"]; } }
        public double Money { get { return DataConverter.CDouble(Request.QueryString["Money"]); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            B_User.CheckIsLogged(Request.RawUrl);
            M_UserInfo mu = buser.GetLogin();
            if (mu.Status != 0) { function.WriteErrMsg("你的帐户未通过验证或被锁定"); }
            if (!IsPostBack)
            {
                M_Payment payMod = new M_Payment();
                DataTable orderDT = new DataTable();
                purseli.Visible = SiteConfig.SiteOption.SiteID.Contains("purse");
                siconli.Visible = SiteConfig.SiteOption.SiteID.Contains("sicon");
                pointli.Visible = SiteConfig.SiteOption.SiteID.Contains("point");
                if (Money > 0)//直接传要充多少，用于充值余额等
                {
                    payMod = CreateChargePay(mu);
                    Response.Redirect("OrderPay.aspx?Payno=" + payMod.PayNo);return;
                }
                else if (!string.IsNullOrEmpty(PayNo))
                {
                    payMod = payBll.SelModelByPayNo(PayNo);
                    OrderHelper.OrdersCheck(payMod);
                    orderDT = orderBll.GetOrderbyOrderNo(payMod.PaymentNum);
                }
                else
                {
                    M_OrderList orderMod = orderBll.SelModelByOrderNo(OrderNo);
                    OrderHelper.CheckIsCanPay(orderMod);
                    orderDT = orderBll.GetOrderbyOrderNo(OrderNo);
                }
                if (orderDT != null && orderDT.Rows.Count > 0)
                {
                    //如果是跳转回来的,检测其是否包含充值订单
                    foreach (DataRow dr in orderDT.Rows)
                    {
                        if (DataConverter.CLng(dr["Ordertype"]) == (int)M_OrderList.OrderEnum.Purse)
                        {
                            virtual_ul.Visible = false; break;
                        }
                    }
                    //总金额,如有支付单,以支付单的为准
                    GetTotal(orderDT, ref price, ref fare, ref allamount);
                    if (!string.IsNullOrEmpty(PayNo))
                    {
                        allamount = (double)payMod.MoneyPay;
                        arrive = payMod.ArriveMoney;
                        point = payMod.UsePoint;
                        point_arrive=payMod.UsePointArrive;
                    }
                    TotalMoney_L.Text = allamount.ToString("f2");
                    TotalMoneyInfo_T.Text = price.ToString("f2") + " + " + fare.ToString("f2") + " - " + arrive.ToString("f2") + "-" + point_arrive.ToString("f2") + "(" + point + ")" + " = " + allamount.ToString("f2");
                    TotalMoneyInfo_T.Text = TotalMoneyInfo_T.Text + "　(商品总价+运费-优惠券-积分兑换=总额)";
                    if (string.IsNullOrEmpty(OrderNo) && Money > 0) { OrderNo_L.Text = "用户充值"; }
                    else if (payMod.PaymentID > 0) { OrderNo_L.Text = payMod.PaymentNum.Trim(','); }
                    else { OrderNo_L.Text = OrderNo; }
                }
                //支付平台
                BindPlat();
                if (payMod.SysRemark == "用户充值") { virtual_ul.Visible = false; }
                //如果已用优惠券|积分,抵扣了金额,则可直接支付
                if (payMod.MoneyReal == 0)
                {
                    FinalStep(payMod);
                }
            }
        }
        public void GetTotal(DataTable dt, ref double price, ref double fare, ref double allamount)
        {
            foreach (DataRow dr in dt.Rows)
            {
                price += Convert.ToDouble(dr["Balance_price"]);
                fare += Convert.ToDouble(dr["Freight"]);
                allamount += Convert.ToDouble(dr["Ordersamount"]);
            }
        }
        public string GetPlatImg()
        {
            string image = "";
            switch (DataConverter.CLng(Eval("PayClass")))
            {
                case (int)M_PayPlat.Plat.Alipay_Instant:
                case (int)M_PayPlat.Plat.Alipay_H5:
                    image = "alipay.jpg";
                    break;
                case (int)M_PayPlat.Plat.Bill:
                    image = "99bill.jpg";
                    break;
                case (int)M_PayPlat.Plat.UnionPay:
                    image = "chinabank.jpg";
                    break;
                case (int)M_PayPlat.Plat.ChinaUnionPay:
                    image = "chinaunion.jpg";
                    break;
                case 15://支付宝单网银
                    //M_PayPlat info = new M_PayPlat();
                    //info = platBll.SelModelByClass(M_PayPlat.Plat.Alipay_Bank);
                    //string[] bankArr = info.PayPlatinfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    //for (int i = 0; i < bankArr.Length; i++)
                    //{
                    //    image = bankArr[i] + ".jpg";
                    //    ListItem newItem = new ListItem("", bankArr[i]);
                    //    newItem.Attributes.Add("id", "td_" + bankArr[i] + "_" + item.Value);
                    //    if (i > 0)
                    //        ids = item.Value + "" + ids;
                    //    DDLPayPlat.Items.Add(newItem);
                    //}
                    //item.Value = "";
                    break;
                case (int)M_PayPlat.Plat.PayPal:
                    image = "paypal.jpg";
                    break;
                case (int)M_PayPlat.Plat.WXPay://微信支付
                    image = "wxpay.jpg";
                    break;
                case (int)M_PayPlat.Plat.ICBC_NC://南昌工行
                    image = "ncicbc.jpg";
                    break;
                case (int)M_PayPlat.Plat.Ebatong:
                    image = "ebatong.jpg";
                    break;
                case (int)M_PayPlat.Plat.CCB:
                    image = "CCB.jpg";
                    break;
                case (int)M_PayPlat.Plat.ECPSS://汇潮
                    image = "ecpss.jpg";
                    break;
                case (int)M_PayPlat.Plat.Offline://线下支付(转账,汇款)
                    image = "offline.jpg";
                    break;
                case (int)M_PayPlat.Plat.CashOnDelivery://货到付款
                    image = "100.jpg";
                    break;
                default:
                    break;
            }
            return "/App_Themes/User/bankimg/" + image;
        }
        //绑定支付平台信息
        public void BindPlat()
        {
            //后期改为Repeater输出
            DataTable dt = platBll.GetPayPlatListAll(0);
            PayPlat_RPT.DataSource = dt;
            PayPlat_RPT.DataBind();
            if (dt == null || dt.Rows.Count < 1) function.WriteErrMsg("尚未定义支付平台");
        }
        //确定,生成信息写入ZL_Payment
        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            int platid = DataConverter.CLng(Request.Form["payplat_rad"]);
            M_UserInfo mu = buser.GetLogin(false);
            M_Payment pinfo = new M_Payment();
            M_OrderList orderMod = new M_OrderList();
            if (!string.IsNullOrEmpty(PayNo))//支付单付款
            {
                pinfo = payBll.SelModelByPayNo(PayNo);
                if (pinfo.Status != (int)M_Payment.PayStatus.NoPay) { function.WriteErrMsg("该支付单已付款,请勿重复支付"); return; }
            }
            else
            {
                DataTable orderDT = orderBll.GetOrderbyOrderNo(OrderNo);
                GetTotal(orderDT, ref price, ref fare, ref allamount);
                if (allamount < 0.01) function.WriteErrMsg("每次划款金额不能低于0.01元");
                if (orderDT != null && orderDT.Rows.Count > 0)
                {
                    orderMod = orderBll.GetOrderListByid(DataConverter.CLng(orderDT.Rows[0]["id"]));
                    orderBll.Update(orderMod);
                }
                pinfo.PaymentNum = OrderNo;
                pinfo.MoneyPay = allamount;
                DataTable cartDT = new DataTable();
                if (orderMod.id > 0)
                {
                    cartDT = bcart.GetCartProOrderID(orderMod.id);
                    pinfo.Remark = cartDT.Rows.Count > 1 ? "[" + cartDT.Rows[0]["ProName"] as string + "]等" : cartDT.Rows[0]["ProName"] as string;
                }
                else { pinfo.Remark = "没有对应订单"; }
            }
            pinfo.UserID = mu.UserID;
            pinfo.PayPlatID = platid;
            pinfo.MoneyID = 0;
            pinfo.MoneyReal = pinfo.MoneyPay;
            //用于支付宝网银
            pinfo.PlatformInfo = Request.Form["payplat_rad"];
            if (!string.IsNullOrEmpty(PayNo)) { payBll.Update(pinfo); }
            else
            {
                pinfo.Status = (int)M_Payment.PayStatus.NoPay;
                pinfo.PayNo = payBll.CreatePayNo();
                payBll.Add(pinfo);
            }
            string url = "PayOnline.aspx?PayNo=" + pinfo.PayNo+"&StoreID="+StoreID;
            if (pinfo.PayPlatID == 0)
            {
                url += "&method=" + Request.Form["payplat_rad"];
            }
            Response.Redirect(url);
        }
        private void FinalStep(M_Payment pinfo)
        {
            if (pinfo.Status != (int)M_Payment.PayStatus.NoPay) { throw new Exception("该支付单已处理过,不可重复执行"); }
            pinfo = payBll.PaySuccess(pinfo, 0, "");
            pinfo.Remark = "";
            foreach (string orderNo in pinfo.PaymentNum.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                M_OrderList orderMod = orderBll.SelModelByOrderNo(orderNo);
                OrderHelper.FinalStep(pinfo, orderMod, null);
            }
            payBll.Update(pinfo);
            Response.Redirect("/User/Order/OrderList");
        }
        //如果是充值,则生成充值支付单
        private M_Payment CreateChargePay(M_UserInfo mu)
        {
            if (Money <=0) { function.WriteErrMsg(""); }
            M_OrderList orderMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Purse);
            orderMod.Ordersamount = Money;
            orderMod.Specifiedprice = Money;
            orderMod.Balance_price = Money;
            orderMod.id = orderBll.insert(orderMod);
            M_Payment payMod = payBll.CreateByOrder(orderMod);
            payMod.MoneyReal = payMod.MoneyPay;
            payMod.PaymentID = payBll.Add(payMod);
            payMod.SysRemark = "用户充值";
            return payMod;
        }
    }
}