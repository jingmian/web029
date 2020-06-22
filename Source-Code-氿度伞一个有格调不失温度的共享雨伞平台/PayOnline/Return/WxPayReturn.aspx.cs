namespace ZoomLaCMS.PayOnline.Return
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using ZoomLa.BLL;
    using ZoomLa.BLL.Shop;
    using ZoomLa.BLL.WxPayAPI;
    using ZoomLa.Components;
    using ZoomLa.Model;
    using ZoomLa.SQLDAL;
    using ZoomLa.BLL.User;
    using ZoomLa.Model.User;
    using ZoomLa.BLL.User.Addon;

    /// <summary>
    /// 微信异步通知页
    /// </summary>
    public partial class WxPayReturn : System.Web.UI.Page
    {
        private string PayPlat = "微信PC扫码|公众号支付";
        ZoomLa.BLL.WxPayAPI.Notify wxnotify = null;
        B_Payment payBll = new B_Payment();
        B_Order_PayLog paylogBll = new B_Order_PayLog();//支付日志类,用于记录用户付款信息
        B_OrderList orderBll = new B_OrderList();
        OrderCommon orderCom = new OrderCommon();
        B_User buser = new B_User();
        protected void Page_Load(object sender, EventArgs e)
        {
            wxnotify = new ZoomLa.BLL.WxPayAPI.Notify(this);
            string result = ProcessNotify();
            Response.Clear(); Response.Write(result); Response.Flush(); Response.End();
        }
        public string ProcessNotify()
        {
            //如果有多个公众号支付,此处要取返回中的公众号ID,再取Key验证
            WxPayData notifyData = wxnotify.GetNotifyData();
            WxPayData res = GetWxDataMod();
            //检查支付结果中transaction_id是否存在
            if (!notifyData.IsSet("out_trade_no"))
            {
                //若transaction_id不存在，则立即返回结果给微信支付后台
                res.SetValue("return_code", "FAIL");
                res.SetValue("return_msg", "支付结果中订单号不存在");
                ZLLog.L(ZLEnum.Log.pay, new M_Log()
                {
                    Action = "支付平台异常",
                    Message = PayPlat + ",原因:支付结果中订单号不存在!XML:" + notifyData.ToXml()
                });
                return res.ToXml();
            }
            string transaction_id = notifyData.GetValue("out_trade_no").ToString();
            //查询订单，判断订单真实性
            //if (!QueryOrder(transaction_id))
            //{
            //    //若订单查询失败，则立即返回结果给微信支付后台
            //    WxPayData res = GetWxDataMod();
            //    res.SetValue("return_code", "FAIL");
            //    res.SetValue("return_msg", "订单查询失败");
            //    ZLLog.L(ZLEnum.Log.pay, new M_Log()
            //    {
            //        Action = "支付平台异常",
            //        Message = PayPlat + ",支付单:" + transaction_id + ",原因:订单查询失败!XML:" + notifyData.ToXml()
            //    });
            //    return res.ToXml();
            //}
            //查询订单成功
            //else
            //{
            //}
            //未指定,则默认加载PC扫码配置
            M_Payment pinfo = payBll.SelModelByPayNo(notifyData.GetValue("out_trade_no").ToString());
            M_WX_APPID appMod = WxPayApi.Pay_GetByID(DataConvert.CLng(pinfo.PlatformInfo));
            notifyData.PayKey = appMod.Pay_Key;
            try
            {
                notifyData.CheckSign();
                PayOrder_Success(pinfo, notifyData);
            }
            catch (Exception ex)
            {
                ZLLog.L(ZLEnum.Log.pay, new M_Log() { Action = PayPlat + "报错,支付单:" + transaction_id, Message = ex.Message + "||XML:" + notifyData.ToXml() });
            }
            res.SetValue("return_code", "SUCCESS");
            res.SetValue("return_msg", "OK");
            return res.ToXml();
        }
        //支付成功时执行的操作
        private void PayOrder_Success(M_Payment pinfo,WxPayData result)
        {
            ZLLog.L(ZLEnum.Log.pay, PayPlat + " 支付单:" + result.GetValue("out_trade_no") + " 金额:" + result.GetValue("total_fee"));
            try
            {
                M_Order_PayLog paylogMod = new M_Order_PayLog();
                if (pinfo == null) { throw new Exception("支付单不存在"); }//支付单检测合为一个方法
                if (pinfo.Status != (int)M_Payment.PayStatus.NoPay) { throw new Exception("支付单状态不为未支付"); }
                pinfo.Status = (int)M_Payment.PayStatus.HasPayed;
                pinfo.PlatformInfo += PayPlat;
                pinfo.SuccessTime = DateTime.Now;
                pinfo.PayTime = DateTime.Now;
                pinfo.CStatus = true;
                //1=100,
                double tradeAmt = Convert.ToDouble(result.GetValue("total_fee")) / 100;
                pinfo.MoneyTrue = tradeAmt;
                payBll.Update(pinfo);
                DataTable orderDT = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);
                foreach (DataRow dr in orderDT.Rows)
                {
                    M_OrderList orderMod = orderBll.SelModelByOrderNo(dr["OrderNo"].ToString());
					OrderHelper.FinalStep(pinfo, orderMod, paylogMod);
                    //发送微信消息
			ZoomLa.BLL.WxAPI wxapi = ZoomLa.BLL.WxAPI.Code_Get();
            ZoomLa.BLL.User.Addon.B_UserAPP appBll = new ZoomLa.BLL.User.Addon.B_UserAPP();
            ZoomLa.Model.User.M_UserAPP appMod = appBll.SelModelByUid(orderMod.Userid,"wechat");
            switch (orderMod.Ordertype)
            {
                case (int)M_OrderList.OrderEnum.Score://押金
                    {
                        if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                        {
                            //发送模板消息通知用户
                            string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您的押金已成功支付！\",\"color\":\"#173177\"}}";
                            wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", json);
                        }
                    }
                    break;
                case (int)M_OrderList.OrderEnum.Purse://充值
                    {
                        if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                        {
                            //发送模板消息通知用户
                            string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"你已成功充值！\",\"color\":\"#173177\"}}";
                            wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", json);
                        }
                    }
                    break;
                case (int)M_OrderList.OrderEnum.Fast://借伞|转借
                    //{
                    //    //转借订单不需要还
                    //    if (orderMod.Promoter == 0) { break; }
                    //    ZoomLa.BLL.Shop.B_Store_Info storeBll = new ZoomLa.BLL.Shop.B_Store_Info();
                    //    ZoomLa.Model.Shop.M_Store_Info storeMod = storeBll.SelReturnModel(orderMod.StoreID);
                    //    if (storeMod != null && storeMod.UserID > 0)
                    //    {
                    //        appMod = appBll.SelModelByUid(storeMod.UserID,"wechat");
                    //        if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                    //        {
                    //            string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户押金已成功支付！\",\"color\":\"#173177\"}}";
                    //            wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/Class_2/NodePage.aspx", json);
                    //        }
                    //    }
                    //}
                    break;
            }




					/*
                    B_UserAPP userappBll = new B_UserAPP();
                    M_UserAPP userappMod = new M_UserAPP();

                    M_UserInfo mu = buser.SeachByID(orderMod.Userid);
                    userappMod = userappBll.SelModelByUid(mu.UserID, "wechat");
                    if (userappMod != null)
                    {
                        WxAPI wxapi = WxAPI.Code_Get(1);
                        string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您的押金已成功支付！\",\"color\":\"#173177\"}}";
                        wxapi.Tlp_SendTlpMsg(userappMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", json);
                    }
					*/
                    //发送消息给店主和送货员
					/*
                    if (orderMod.StoreID > 0)
                    {
                        B_Content contBll = new B_Content();
                        M_CommonData CData = contBll.SelReturnModel(orderMod.StoreID);
                        if (CData != null)
                        {
                            //店主
                            M_UserInfo smu = buser.GetUserIDByUserName(CData.Inputer);
							userappMod = userappBll.SelModelByUid(smu.UserID, "wechat");
                            if (smu != null && smu.UserID > 0)
                            {
                                string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户押金已成功支付！\",\"color\":\"#173177\"}}";
								wxapi.Tlp_SendTlpMsg(userappMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", SiteConfig.SiteInfo.SiteUrl + "/Class_2/NodePage.aspx", json);
                            }
                        }
                    }
                    */
                    //if (orderMod.Ordertype == (int)M_OrderList.OrderEnum.Purse)
                    //{

                    //    M_UserInfo mu = buser.SelReturnModel(orderMod.Userid);
                    //    new B_Shop_MoneyRegular().AddMoneyByMin(mu, orderMod.Ordersamount, ",订单号[" + orderMod.OrderNo + "]");
                    //}
                    //orderCom.SendMessage(orderMod, paylogMod, "payed");
                    //orderCom.SaveSnapShot(orderMod);
                }
                ZLLog.L(ZLEnum.Log.pay, PayPlat + "成功!支付单:" + result.GetValue("out_trade_no").ToString());
            }
            catch (Exception ex)
            {
                ZLLog.L(ZLEnum.Log.pay, new M_Log()
                {
                    Action = "支付回调报错",
                    Message = PayPlat + ",支付单:" + result.GetValue("out_trade_no").ToString() + ",原因:" + ex.Message
                });
            }
        }
        //查询订单
        private bool QueryOrder(string transaction_id)
        {
            WxPayData req = GetWxDataMod();
            req.SetValue("out_trade_no", transaction_id);
            WxPayData res = WxPayApi.OrderQuery(req, WxPayApi.Pay_GetByID());
            if (res.GetValue("return_code").ToString() == "SUCCESS" &&
                res.GetValue("result_code").ToString() == "SUCCESS")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private WxPayData GetWxDataMod() { return new WxPayData(); }
    }
}