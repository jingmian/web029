namespace ZoomLaCMS.PayOnline
{
    using System;
    using System.Data;
    using System.Configuration;
    using System.Collections;
    using System.Web;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Web.UI.HtmlControls;
    using ZoomLa.BLL;
    using ZoomLa.Common;
    using ZoomLa.Model;
    using ZoomLa.SQLDAL;
    using ZoomLa.BLL.Shop;
    public partial class SelectPayPlat : System.Web.UI.Page
    {
        protected B_Shop_MoneyRegular moneyBll = new B_Shop_MoneyRegular();
        protected void Page_Load(object sender, EventArgs e)
        {
            B_User.CheckIsLogged(Request.RawUrl);
            MyBind();
        }
        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            double money = DataConverter.CDouble(Money_T.Text.Trim());
            //if (money < 1) { function.WriteErrMsg("充值金额过小"); return; }
            Response.Redirect("~/PayOnline/OrderPay.aspx?Money=" + money);
        }
        protected void MyBind()
        {
            DataTable dt = new DataTable();
            dt = moneyBll.Sel();
            RPT.DataSource = dt;
            RPT.DataBind();
        }
        public string GetIcon()
        {
            double price = DataConvert.CDouble(Eval("Min").ToString());
            int num = DataConvert.CLng(price / 100);
            num = num <= 0 ? 1 : num;
            return num.ToString();
        }
    }
}