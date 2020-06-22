using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.SQLDAL;
using ZoomLa.SQLDAL.SQL;

namespace ZoomLaCMS.BU.UE.Admin
{
    public partial class OrderList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            RPT.SPage = MyBind;
            if (!IsPostBack)
            {
                RPT.DataBind();
            }
        }
        private DataTable MyBind(int pageSize, int pageIndex)
        {
            F_Unbrealla filter = new F_Unbrealla() { status = 0 };
            filter.type = Type_DP.SelectedValue;
            filter.status = DataConvert.CLng(Status_DP.SelectedValue);
            filter.uname = UName_T.Text.Trim();
            PageSetting config = SelPage(pageIndex, pageSize, filter);
            RPT.ItemCount = config.itemCount;
            return config.dt;
        }

        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {

        }
        public PageSetting SelPage(int cpage, int psize, F_Unbrealla filter)
        {
            string tbview = "ZL_Ex_OrderView";
            string where = "1=1 ";
            List<SqlParameter> sp = new List<SqlParameter>();
            switch (filter.type)
            {
                case "user":
                    where += " AND Promoter>0";
                    break;
                case "store":
                    where += " AND Promoter=0";
                    break;
                case "all":
                default:
                    break;
            }
            if (filter.status != -100)
            {
                where += " AND OrderStatus=" + filter.status;
            }
            if (!string.IsNullOrEmpty(filter.uname))
            {
                where += " AND (HoneyName LIKE @uname OR [Permissions] LIKE @uname) ";
                sp.Add(new SqlParameter("uname", "%" + filter.uname + "%"));
            }
            if (filter.storeID != -100)
            {
                where += " AND StoreID=" + filter.storeID;
            }
            PageSetting config = PageSetting.Single(cpage, psize, tbview, "ID", where, "ID DESC", sp);
            DBCenter.SelPage(config);
            return config;
        }

        protected void Skey_Btn_Click(object sender, EventArgs e)
        {
            RPT.DataBind();
        }
    }
    public class F_Unbrealla
    {
        //订单所处状态
        public string type = "all";
        public int status = -100;
        public string uname = "";
        public int storeID = -100;
    }
}