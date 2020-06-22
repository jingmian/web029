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
    public partial class ForeGift : System.Web.UI.Page
    {
        B_OrderList orderBll = new B_OrderList();
        B_Payment payBll = new B_Payment();
        B_User buser = new B_User();
        M_UserInfo mu = null;
        public int Num { get { return DataConvert.CLng(Request.QueryString["num"], 1); } }
        public int StoreID { get { return DataConvert.CLng(Request["StoreID"],1); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            mu = buser.GetLogin();
            if (!IsPostBack)
            {
                double money = Unbrealla.T_CheckForegift(mu, Num);
                Foregift_L.Text = money.ToString("F2");
            }
        }

        protected void Charge_Btn_Click(object sender, EventArgs e)
        {
            double money = Unbrealla.T_CheckForegift(mu, Num);
            M_OrderList foreMod = Unbrealla.H_NewFGOrder(mu, money);
            M_Payment payMod = Unbrealla.H_NewFGPayment(foreMod);
            orderBll.insert(foreMod);
            payMod.PaymentID = payBll.Add(payMod);
            Response.Redirect("/PayOnline/OrderPay.aspx?Payno=" + payMod.PayNo + "&StoreID=" + StoreID);
        }
    }
}