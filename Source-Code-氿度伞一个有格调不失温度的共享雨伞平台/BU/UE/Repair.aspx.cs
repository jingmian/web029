using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.Sns;
using ZoomLa.SQLDAL;
using ZoomLa.SQLDAL.SQL;

namespace ZoomLaCMS.BU.UE
{
    public partial class Repair : System.Web.UI.Page
    {
        M_UserInfo mu = null;
        B_User buser = new B_User();
        B_OrderList orderBll = new B_OrderList();
        B_Payment payBll = new B_Payment();
        public int StoreID { get { return DataConvert.CLng(Request["StoreID"]); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            mu = buser.GetLogin(false);
            if (!IsPostBack)
            {
                if (StoreID < 1) { function.WriteErrMsg("未指定还伞网点"); }
                MyBind();
            }
        }
        private void MyBind()
        {
            RPT.DataSource = SelMyLend();
            RPT.DataBind();
        }

        protected void BatReturn_Btn_Click(object sender, EventArgs e)
        {
            string ids = Request.Form["idchk"];
            if (string.IsNullOrEmpty(ids)) { function.WriteErrMsg("未选择需要归还的伞"); }
            SafeSC.CheckIDSEx(ids);
            //计算金额,生成余额充值订单
            double money = 0;
            foreach (string id in ids.Split(','))
            {
                int rid = DataConvert.CLng(Request["repair" + id + "_rad"]);
                DataRow dr = Conast.Repair_Get(rid);
                money += DataConvert.CDouble(dr["price"]);
                string msg = dr["name"] + " " + Convert.ToDouble(dr["price"]) + "元";
                DBCenter.UpdateSQL("ZL_OrderInfo", "Internalrecords='" + msg + "',Specifiedprice='" + dr["price"] + "'", "ID=" + id);
            }
            if (money < 1) { function.WriteErrMsg("充值金额不正确"); }
            if ((mu.Purse - 9) >= money)
            {
                //余额充足,则直接修改金额
                Unbrealla.RepairOrder(ids, mu.UserID, StoreID, money);
                function.WriteSuccessMsg("报修成功,请等待店主审核");
            }
            else
            {
                //缺多少充多少
                M_OrderList orderMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Purse);
                orderMod.Ordersamount = money - (mu.Purse - 9);
                orderMod.Ordermessage = "报修充值";
                orderMod.StoreID = StoreID;
                //money用于检测充完值后,金额是否达标
                orderMod.Freight_remark = JsonHelper.GetJson(new string[] { "type", "ids", "money" },
                    new string[] { "repair", ids, money.ToString("F2") });
                M_Payment payMod = payBll.CreateByOrder(orderMod);
                orderBll.insert(orderMod);
                payBll.Add(payMod);
                Response.Redirect("/Payonline/wxpay_mp.aspx?payno=" + payMod.PayNo);
            }
        }
        //只取正常状态的
        private DataTable SelMyLend()
        {
            string where = "UserID=" + mu.UserID + " AND OrderType=10 AND OrderStatus IN (0,-2) ";
            PageSetting setting = PageSetting.Single(1, int.MaxValue, "ZL_Ex_OrderView", "ID", where, "OrderStatus DESC,ID ASC");
            DBCenter.SelPage(setting);
            return setting.dt;
        }

        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {

        }
    }
}