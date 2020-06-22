using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Model;
using ZoomLa.Model.Shop;
using ZoomLa.Sns;
using ZoomLa.SQLDAL;
using ZoomLa.BLL.User;
using ZoomLa.Model.User;
using ZoomLa.BLL.User.Addon;
using System.Data.SqlClient;
using ZoomLa.BLL.API;
using ZoomLa.BLL.CreateJS;
using ZoomLa.BLL.Helper;
using ZoomLa.Model.CreateJS;
using Newtonsoft.Json;
using ZoomLa.SQLDAL.SQL;
using ZoomLa.Common;
using ZoomLa.BLL.Shop;

namespace ZoomLaCMS.BU.UE.Store
{
    public partial class ReturnList : System.Web.UI.Page
    {
        B_User buser = new B_User();
        B_UserAPP appBll = new B_UserAPP();
        B_OrderList orderBll = new B_OrderList();
        B_Product proBll = new B_Product();
        M_UserInfo mu = null;
        M_Store_Info storeMod = null;
        //显示已提交归还申请的伞,可在不同的店铺归还
        protected void Page_Load(object sender, EventArgs e)
        {
            mu = buser.GetLogin();
            storeMod= Unbrealla.U_IsStore(mu);
            if (!IsPostBack)
            {
                MyBind();
            }
        }
        private void MyBind()
        {
            //归还到指定商家,且未确认
            string where = "Settle=" + storeMod.ID + " AND OrderStatus IN (-1,-2)";
            DataTable dt = DBCenter.Sel("ZL_Ex_OrderView",where);
            RPT.DataSource = dt;
            RPT.DataBind();
        }


        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            WxAPI wxapi = WxAPI.Code_Get();
            int oid = DataConvert.CLng(e.CommandArgument);
            M_OrderList orderMod = orderBll.SelReturnModel(oid);
		    M_UserAPP appMod = appBll.SelModelByUid(orderMod.Userid, "wechat");
            M_UserAPP appMod2 = appBll.SelModelByUid(storeMod.UserID, "wechat");
            string tlpid = "dlAIEAkV0lgDfc7-RYsdLVFMyP9UF7gHi_9kjPblVAU";
            switch (e.CommandName)
            {
                case "return"://确认归还
                    {
                        Unbrealla.FinalOrder(orderMod);
									if(appMod!=null && !string.IsNullOrEmpty(appMod.OpenID)){
							string json = "{\"first\": {\"value\":\"还伞成功通知\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + orderMod.AddTime.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您已还伞成功，如需继续借伞，请扫描借伞点二维码进行借伞！\",\"color\":\"#173177\"}}";
							string r=wxapi.Tlp_SendTlpMsg(appMod.OpenID, tlpid, ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/BU/UE/MyLend.aspx", json);
						}
						if(appMod2!=null && !string.IsNullOrEmpty(appMod2.OpenID)){
							string json1 = "{\"first\": {\"value\":\"还伞成功通知\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + orderMod.AddTime.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户已还伞成功！\",\"color\":\"#173177\"}}";
							wxapi.Tlp_SendTlpMsg(appMod2.OpenID,tlpid, ZoomLa.Components.SiteConfig.SiteInfo.SiteUrl + "/Class_2/NodePage.aspx", json1);
						}
                        
                        //更新库存
                        string sql = "select * from ZL_Commodities where UserShopID=" + orderMod.StoreID;
                        DataTable dt = SqlHelper.ExecuteTable(CommandType.Text,sql);
                        if(dt.Rows.Count>0){
                            M_Product proMod = proBll.GetproductByid(DataConvert.CLng(dt.Rows[0]["ID"]));
                            proMod.Stock = proMod.Stock + 1;
                            proBll.UpdateByID(proMod);
                        }
                    }
                    break;
                case "cancel"://取消归还
                    {
                        DBCenter.UpdateSQL(orderMod.TbName, "OrderStatus=0", "ID=" + oid + " AND OrderStatus=-1");
                    }
                    break;
                case "repair"://坏伞回收
                    {
                        Unbrealla.FinalOrder(orderMod);
                        string json = "{\"first\": {\"value\":\"还伞成功通知\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + orderMod.AddTime.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"您已还伞成功，如需继续借伞，请扫描借伞点二维码进行借伞！\",\"color\":\"#173177\"}}";
                        Unbrealla.SendWXNotify(appMod, tlpid, "/BU/UE/MyLend.aspx", json);
                        json = "{\"first\": {\"value\":\"还伞成功通知\",\"color\":\"#173177\"},\"keyword1\":{\"value\":\"" + orderMod.OrderNo + "\",\"color\":\"#173177\"},\"keyword2\":{\"value\":\"" + orderMod.AddTime.ToString("yyyy年MM月dd日 HH:mm:ss") + "\",\"color\":\"#173177\"},\"keyword3\": {\"value\":\"" + orderMod.Ordersamount.ToString("0.00") + "元\",\"color\":\"#173177\"},\"remark\":{\"value\":\"用户已还伞成功，请等待公司工作人员收取坏伞！\",\"color\":\"#173177\"}}";
                        Unbrealla.SendWXNotify(appMod2, tlpid, "/Class_2/NodePage.aspx", json);
                    }
                    break;
                case "cancel_repair"://取消报修申请,返还余额
                    {
                        double money = orderMod.Specifiedprice;
                        orderMod.IsCount = false;
                        orderMod.Internalrecords = "";
                        orderMod.Specifiedprice = 0;
                        orderMod.OrderStatus = 0;
                        orderBll.UpdateByID(orderMod);
                        buser.AddMoney(orderMod.Userid, money, M_UserExpHis.SType.Purse, "取消报修,返还预扣余额["+orderMod.id+"]");
                    }
                    break;
            }
            MyBind();
        }
    }
}