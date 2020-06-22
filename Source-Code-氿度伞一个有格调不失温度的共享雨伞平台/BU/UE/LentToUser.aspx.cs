using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.Sns;
using ZoomLa.SQLDAL;

namespace ZoomLaCMS.BU.UE
{
    public partial class LentToUser : System.Web.UI.Page
    {
        B_User buser = new B_User();
        B_OrderList orderBll = new B_OrderList();
        B_Payment payBll = new B_Payment();
        public int Oid { get { return DataConvert.CLng(Request.QueryString["OID"]); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            B_User.CheckIsLogged(Request.RawUrl);
            if (!IsPostBack)
            {
                int num = 1;
                M_OrderList orderMod = orderBll.SelReturnModel(Oid);
                M_UserInfo frommu = buser.SelReturnModel(orderMod.Userid);
                M_UserInfo mu = buser.GetLogin(false);
                //检测订单是否可借
                if (orderMod == null) { HttpContext.Current.Response.Write("<script>alert('订单不存在')</script>"); }
                if (orderMod.Ordertype != (int)M_OrderList.OrderEnum.Fast) { HttpContext.Current.Response.Write("<script>alert('订单不可操作')</script>"); }
                if (orderMod.OrderStatus != 0) { HttpContext.Current.Response.Write("<script>alert('订单状态不正确')</script>"); }
                if (mu.UserID == orderMod.Userid) { HttpContext.Current.Response.Write("<script>alert('不可转借给本人')</script>"); }


                //=================================检测是否符合条件
                double money = Unbrealla.T_CheckForegift(mu, num);
                //---------需要充值押金
                string json = JsonHelper.GetJson("oid,action".Split(','),
                    (orderMod.id + ",ue_lenttouser").Split(','));
                if (money > 0)
                {
                    M_OrderList foreMod = Unbrealla.H_NewFGOrder(mu, money);
                    foreMod.StoreID = orderMod.StoreID;
                    M_Payment forePay = Unbrealla.H_NewFGPayment(foreMod);
                    forePay.SysRemark = json;
                    orderBll.insert(foreMod);
                    payBll.Add(forePay);
                    Response.Redirect("/PayOnline/wxpay_mp.aspx?payno=" + forePay.PayNo); return;
                }
                //---------余额必须为正数,否则必须充值
                if (mu.Purse <= 0)
                {
                    //9,19,50,100
                    M_OrderList chargeMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Purse);
                    chargeMod.Ordersamount = 9;
                    chargeMod.StoreID = orderMod.StoreID;
                    M_Payment payMod = payBll.CreateByOrder(chargeMod);
                    payMod.SysRemark = json;
                    payMod.Remark += "充值" + chargeMod.Ordersamount.ToString("F2") + "元";
                    orderBll.insert(chargeMod);
                    payMod.PaymentID = payBll.Add(payMod);
                    Response.Redirect("/PayOnline/wxpay_mp.aspx?payno=" + payMod.PayNo); return;
                }
                //=================================

                //终止上一份订单,并生成新的转借订单
                orderMod.Ordermessage = "转借";
                Unbrealla.FinalOrder(orderMod);
                //创建新的订单
                M_OrderList newMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Fast);
                newMod.Balance_remark = orderMod.Balance_remark;
                newMod.Promoter = frommu.UserID;
                newMod.StoreID = orderMod.StoreID;
                newMod.Extend = DateTime.Now.ToString();
                newMod.id = orderBll.insert(newMod);
                
                //新用户
                mu = buser.GetLogin(false);
                if (mu.PageID == 0)
                {
                    buser.AddMoney(orderMod.Userid, 5, M_UserExpHis.SType.Purse, "新用户转借奖励,订单[" + newMod.id + "]");
                    mu.PageID = 1;
                    buser.UpdateByID(mu);
                    WxAPI wxapi = WxAPI.Code_Get();
                    M_UserAPP appMod = appBll.SelModelByUid(orderMod.Userid, "wechat");
                    if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                    {
                        string tlp1 = "{\"first\": {\"value\":\"奖励到帐提醒\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"转借5元奖励已到账，该奖励可提现，请注意查收账户内余额\",\"color\":\"#173177\"}}";
                        wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", tlp1);
                    }
                }
                HttpContext.Current.Response.Write("<script>alert('操作成功')</script>");
                Response.Redirect("/BU/UE/MyLend.aspx");
            }
        }
    }
}