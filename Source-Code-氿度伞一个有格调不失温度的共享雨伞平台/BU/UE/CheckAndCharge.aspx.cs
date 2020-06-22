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
    public partial class CheckAndCharge : System.Web.UI.Page
    {
        B_User buser = new B_User();
        B_OrderList orderBll = new B_OrderList();
        B_Payment payBll = new B_Payment();
        public int Num { get { return DataConvert.CLng(Request["num"],1); } }
        public int StoreID { get { return DataConvert.CLng(Request["StoreID"],1); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                M_UserInfo mu = buser.GetLogin(false);
                //double money = Unbrealla.T_CheckForegift(mu,Num);
                //if (money > 0) { Response.Redirect("/BU/UE/ForeGift.aspx?num="+Num); }
            }
        }

        protected void Pay_Btn_Click(object sender, EventArgs e)
        {
            M_UserInfo mu = buser.GetLogin();
            double charge = DataConvert.CLng(Request.Form["charge_rad"]);
            M_OrderList chargeMod = orderBll.NewOrder(mu, M_OrderList.OrderEnum.Purse);
            chargeMod.Ordersamount = charge;
            M_Payment payMod = payBll.CreateByOrder(chargeMod);
            payMod.Remark += "充值" + chargeMod.Ordersamount.ToString("F2") + "元";
            if (payMod.MoneyReal < 1) { function.WriteErrMsg("充值金额错误"); }
            orderBll.insert(chargeMod);
            payMod.PaymentID = payBll.Add(payMod);
            Response.Redirect("/PayOnline/OrderPay.aspx?Payno=" + payMod.PayNo + "&StoreID=" + StoreID);
        }
    }
}