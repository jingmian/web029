<%@ WebHandler Language="C#" Class="client" %>
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using ZoomLa.BLL;
using ZoomLa.BLL.API;
using ZoomLa.BLL.CreateJS;
using ZoomLa.BLL.Helper;
using ZoomLa.Model;
using ZoomLa.Model.CreateJS;
using ZoomLa.SQLDAL;
using Newtonsoft.Json;
using ZoomLa.SQLDAL.SQL;
using ZoomLa.BLL.User.Addon;
using ZoomLa.Model.User;
using ZoomLa.Sns;
using ZoomLa.Common;
using ZoomLa.BLL.Shop;
using ZoomLa.Model.Shop;
public class client : API_Base, IHttpHandler
{
    B_User buser = new B_User();
    B_UserAPP appBll = new B_UserAPP();
    B_OrderList orderBll = new B_OrderList();
    B_Payment payBll = new B_Payment();
    B_Product proBll = new B_Product();
    B_Stock skBll = new B_Stock();
    B_Store_Info storeBll = new B_Store_Info();
    M_UserInfo mu = new M_UserInfo();
    public void ProcessRequest(HttpContext context)
    {
        retMod.retcode = M_APIResult.Failed;
        retMod.callback = "";//禁止jsonp
        HttpRequest req = context.Request;
        mu = buser.GetLogin(false);
        string err = "";
        if (mu.IsNull) { retMod.retmsg = "用户未登录"; RepToClient(retMod); return; }
        if (!B_User.CheckUserStatus(mu, ref err)) { retMod.retmsg = err; RepToClient(retMod); return; }
        //-----------------------------
        try
        {
            switch (Action.ToLower())//改为不匹分大小写
            {
                case "isfollow":
                    {
                        //不在微信中则不检测
                        if (DeviceHelper.GetBrower() != DeviceHelper.Brower.Micro)
                        {
                            retMod.retcode = M_APIResult.Success;
                            retMod.retmsg = "非微信浏览器";
                        } 
                        else
                        {
                            WxAPI wxapi = WxAPI.Code_Get();
                            if (wxapi.IsFollow(mu.UserID)) { retMod.retcode = M_APIResult.Success; }
                            else { retMod.retmsg = "用户未关注"; }
                        }
                    }
                    break;
                case "checkmoney"://用户点击发起借伞,检测是否符合条件
                    {
                        int num = DataConvert.CLng(Req("num"), 1);
                        CommonReturn ret = Unbrealla.T_IsAllowLend(mu, num);
                        if (ret.isok) { retMod.retcode = M_APIResult.Success; }
                        else { retMod.retmsg = ret.err; }
                    }
                    break;
                case "ue_lend"://向商家发起借伞,生成借伞订单
                    {
                        //此处让其提供ID
                        int storeID = DataConvert.CLng(Req("storeID"));
                        int num = DataConvert.CLng(Req("num"), 1);
                        int proid = DataConvert.CLng(Req("proid"));
                        if (storeID < 1) { retMod.retmsg = "店铺标识错误"; RepToClient(retMod); return; }
                        if (proid < 1) { proid = DataConvert.CLng(DBCenter.ExecuteScala("ZL_Commodities", "ID", "UserShopID=" + storeID)); }
                        //=================================检测是否符合条件
                        double money = Unbrealla.T_CheckForegift(mu, num);
                        //---------需要充值押金
                        string json = JsonHelper.GetJson("storeid,proid,num,action".Split(','),
                            (storeID + "," + proid + "," + num + ",ue_lend").Split(','));
                        if (money > 0)
                        {
                            M_OrderList foreMod = Unbrealla.H_NewFGOrder(mu, money);
                            foreMod.StoreID = storeID;
                            M_Payment forePay = Unbrealla.H_NewFGPayment(foreMod);
                            forePay.SysRemark = json;
                            orderBll.insert(foreMod);
                            payBll.Add(forePay);
                            retMod.retcode = M_APIResult.Failed;
                            retMod.result = "{\"type\":1,\"payno\":\"" + forePay.PayNo + "\"}";
                            RepToClient(retMod); return;
                        }
                        //---------余额必须为正数,否则必须充值
                        if (mu.Purse <= 0)
                        {
                            //9,19,50,100
                            M_OrderList chargeMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Purse);
                            chargeMod.Ordersamount = 9;
                            chargeMod.StoreID = storeID;
                            M_Payment payMod = payBll.CreateByOrder(chargeMod);
                            payMod.SysRemark = json;
                            payMod.Remark += "充值" + chargeMod.Ordersamount.ToString("F2") + "元";
                            orderBll.insert(chargeMod);
                            payMod.PaymentID = payBll.Add(payMod);
                            retMod.retcode = M_APIResult.Failed;
                            retMod.result = "{\"type\":2,\"payno\":\"" + payMod.PayNo + "\"}";
                            RepToClient(retMod); return;
                        }
                        //=================================
                        //更新库存
                        M_Product proMod = proBll.GetproductByid(proid);
                        M_Store_Info storeMod = storeBll.SelReturnModel(storeID);
                        if (proMod == null) { retMod.retmsg = "商品[" + proid + "]不存在"; RepToClient(retMod); return; }
                        if (proMod.Stock <= 0) { retMod.retmsg = "伞已全部借出了"; RepToClient(retMod); return; }
                        proMod.Stock = proMod.Stock - num;
                        proBll.UpdateByID(proMod);

                        //----------------------------------
                        M_OrderList orderMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Fast);
                        orderMod.StoreID = storeID;
                        orderMod.Extend = DateTime.Now.ToString();
                        for (int i = 0; i < num; i++)
                        {
                            orderMod.Balance_remark = function.GetRandomString(6, 2);
                            orderMod.id = orderBll.insert(orderMod);
                        }
                        mu = buser.GetLogin(false);
                        if (mu.PageID == 0)
                        {
                            buser.AddMoney(storeMod.UserID, 3, M_UserExpHis.SType.Purse, "新用户借出奖励,订单[" + orderMod.id + "]");
                        }
                        retMod.result = "借出成功";
                        retMod.retcode = M_APIResult.Success;
                        //发送通知用户
                        WxAPI wxapi = WxAPI.Code_Get();
                        M_UserAPP appMod = appBll.SelModelByUid(mu.UserID, "wechat");
                        if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                        {
                            string tlp1 = "{\"first\": {\"value\":\"借伞成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您已借伞成功，您有24小时免费体验时间，感谢您使用氿度伞！\",\"color\":\"#173177\"}}";
                            wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", tlp1);
                        }
                        //店主
                        appMod = appBll.SelModelByUid(storeMod.UserID, "wechat");
                        if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                        {
                            string tlp1 = "{\"first\": {\"value\":\"出借成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户借伞成功，您的收益将在48小时后到账！\",\"color\":\"#173177\"}}";
                            wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/Class_2/NodePage.aspx", tlp1);
			    if(mu.PageID == 0)
			    {
			        mu.PageID = 1;
			        tlp1 = "{\"first\": {\"value\":\"奖金又来了\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"借伞3元奖励已到账，该奖励可提现，请注意查收账户内余额\",\"color\":\"#173177\"}}";
                                wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/User/Info/ConsumeDetail?SType=1", tlp1);
				buser.UpdateByID(mu);
			    }
                        }
                    }
                    break;
                case "ue_return"://还伞
                    {
                        int oid = DataConvert.CLng(Req("oid"));
                        M_OrderList orderMod = orderBll.SelReturnModel(oid);
                        CommonReturn ret = CheckOrder(orderMod);
                        if (ret.isok)
                        {
                            //用户申请还伞(需要店主确认还伞,有一个还伞确认列表)
                            orderMod.OrderStatus = (int)M_OrderList.StatusEnum.DrawBack;
                            DBCenter.UpdateSQL(orderMod.TbName, "OrderStatus=" + orderMod.OrderStatus,
                                "ID=" + orderMod.id);
                            retMod.retcode = M_APIResult.Success;
                        }
                        else
                        {
                            retMod.retmsg = ret.err;
                        }
                    }
                    break;
                case "payisok"://押金|充值订单是否成功
                    {
                        string payno = Req("payno");
                        M_Payment payMod = payBll.SelModelByPayNo(payno);
                        M_OrderList orderMod = orderBll.SelModelByOrderNo(payMod.PaymentNum);
                        if (orderMod.OrderStatus == 99)
                        {
                            retMod.retcode = M_APIResult.Success;
                        }
                    }
                    break;
                default:
                    retMod.retcode = M_APIResult.Failed;
                    retMod.retmsg = Action + "不是有效的参数";
                    break;
            }
        }
        catch (Exception ex) { retMod.retcode = M_APIResult.Failed; retMod.retmsg = ex.Message; }
        RepToClient(retMod);
    }
    //检测是否可转借
    public CommonReturn CheckOrder(M_OrderList orderMod)
    {
        string err = "";
        if (orderMod == null) { err = "订单不存在"; }
        else if (orderMod.Userid != mu.UserID) { err = "你没有操作该订单的权限"; }
        else if (orderMod.OrderStatus != 0) { err = "该订单不可操作"; }
        if (string.IsNullOrEmpty(err)) { return CommonReturn.Success(); }
        else { return CommonReturn.Failed(err); }
    }
    public bool IsReusable { get { return false; } }
}