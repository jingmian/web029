using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Model;
using ZoomLa.SQLDAL;
using ZoomLa.SQLDAL.SQL;

namespace ZoomLaCMS.Tools
{
    public partial class Example : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            /*
			M_OrderList orderMod = new B_OrderList().SelReturnModel(422);
            ZoomLa.BLL.WxAPI wxapi = ZoomLa.BLL.WxAPI.Code_Get();
            ZoomLa.BLL.User.Addon.B_UserAPP appBll = new ZoomLa.BLL.User.Addon.B_UserAPP();
            ZoomLa.Model.User.M_UserAPP appMod = appBll.SelModelByUid(orderMod.Userid,"wechat");
            if (appMod != null&&!string.IsNullOrEmpty(appMod.OpenID))
            {
                //发送模板消息通知用户
                string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您的押金已成功支付！\",\"color\":\"#173177\"}}";
                wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", json);
            }
            switch (orderMod.Ordertype)
            {
                case (int)M_OrderList.OrderEnum.Score://押金
                    break;
                case (int)M_OrderList.OrderEnum.Purse://充值
                    break;
                case (int)M_OrderList.OrderEnum.Fast://借伞|转借
                    {
                        //转借订单不需要还
                        if (orderMod.Promoter == 0) { break; }
                        ZoomLa.BLL.Shop.B_Store_Info storeBll = new ZoomLa.BLL.Shop.B_Store_Info();
                        ZoomLa.Model.Shop.M_Store_Info storeMod = storeBll.SelReturnModel(orderMod.StoreID);
                        if (storeMod != null && storeMod.UserID > 0)
                        {
                            appMod = appBll.SelModelByUid(storeMod.UserID,"wechat");
                            if (appMod != null && !string.IsNullOrEmpty(appMod.OpenID))
                            {
                                string json = "{\"first\": {\"value\":\"订单支付成功\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户押金已成功支付！\",\"color\":\"#173177\"}}";
                                wxapi.Tlp_SendTlpMsg(appMod.OpenID, "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU", ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/Class_2/NodePage.aspx", json);
                            }
                        }
                    }
                    break;
            }
			*/
        }
    }
}