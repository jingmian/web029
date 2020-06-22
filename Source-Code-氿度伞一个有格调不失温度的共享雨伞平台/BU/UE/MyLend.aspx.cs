using System;
using System.Web;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.SQLDAL;
using ZoomLa.SQLDAL.SQL;

namespace ZoomLaCMS.BU.UE
{
    public partial class MyLend : System.Web.UI.Page
    {
        B_OrderList orderBll = new B_OrderList();
        B_User buser = new B_User();
        M_UserInfo mu = null;

        public int StoreID { get { return DataConvert.CLng(Request["StoreID"]); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            B_User.CheckIsLogged(Request.RawUrl);
            mu = buser.GetLogin();
            if (!IsPostBack)
            {
                MyBind();
            }
        }
        private void MyBind()
        {
            M_UserInfo mu = buser.GetLogin();
            DataTable dt = SelMyLend(1000);
            if (dt.Rows.Count < 1||StoreID<1) { op_div.Visible = false; }
            RPT.DataSource = dt;
            RPT.DataBind();
        }
        protected void BatReturn_Btn_Click(object sender, EventArgs e)
        {
            int num = DataConvert.CLng(Num_Hid.Value);
            if (num < 1 || num > 1000) { HttpContext.Current.Response.Write("<script>alert('数量不正确')</script>"); }
            DataTable dt = SelMyLend(num);
            string ids = "";
            foreach (DataRow dr in dt.Rows)
            {
                ids += dr["ID"] + ",";
            }
            ids = ids.TrimEnd(',');
            if (string.IsNullOrEmpty(ids)) { return; }
            DBCenter.UpdateSQL("ZL_OrderInfo", "Settle=" + StoreID + ",OrderStatus=" + (int)M_OrderList.StatusEnum.DrawBack,
                               "ID IN (" + ids + ")");
            MyBind();
        }

        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            M_UserInfo mu = buser.GetLogin();
            switch (e.CommandName)
            {
                case "return":
                    {
                        if (StoreID < 1) { HttpContext.Current.Response.Write("<script>alert('请扫描商户二维码进行还伞操作')</script>"); }
                        int oid = DataConvert.CLng(e.CommandArgument);
                        M_OrderList orderMod = orderBll.SelReturnModel(oid);
                        if (orderMod.OrderStatus != 0) { HttpContext.Current.Response.Write("<script>alert('订单状态不正确')</script>"); }
                        if (orderMod.Userid != mu.UserID) { HttpContext.Current.Response.Write("<script>alert('你无权操作该订单')</script>"); }
                        orderMod.Settle = StoreID;
                        DBCenter.UpdateSQL("ZL_OrderInfo", "Settle=" + StoreID + ",OrderStatus=" + (int)M_OrderList.StatusEnum.DrawBack, "ID=" + oid);
                        HttpContext.Current.Response.Write("<script>alert('还伞提交成功，请等待商户确认')</script>");
                    }
                    break;
            }
            MyBind();
        }
        private DataTable SelMyLend(int num)
        {
            string where = "UserID=" + mu.UserID + " AND OrderType=10 AND OrderStatus IN (0,-1) ";
            PageSetting setting = PageSetting.Single(1, num, "ZL_Ex_OrderView", "ID",where, "OrderStatus DESC,ID ASC");
            DBCenter.SelPage(setting);
            setting.dt.DefaultView.RowFilter = "OrderStatus=0";
            MaxCount_Hid.Value = setting.dt.DefaultView.Table.Rows.Count.ToString();
            setting.dt.DefaultView.RowFilter = "";
            return setting.dt;
        }
    }
}