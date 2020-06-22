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

namespace ZoomLaCMS.BU.UE.Store
{
    public partial class MyLiveLend : System.Web.UI.Page
    {
        B_User buser = new B_User();
        M_UserInfo mu = null;
        M_Store_Info storeMod = null;
        //我借出的仍在生效,且未转借的伞
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
            //从该店铺借出,未转借的伞
            string where = "StoreID=" + storeMod.ID + " AND OrderStatus IN (0,-1) AND OrderType=10 AND Promoter=0";
            DataTable dt = DBCenter.Sel("ZL_OrderInfo", where);
            RPT.DataSource = null;
            RPT.DataBind();
        }

        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {

        }
    }
}