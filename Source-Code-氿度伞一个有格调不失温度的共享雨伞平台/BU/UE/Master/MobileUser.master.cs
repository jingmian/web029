using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;

namespace ZoomLaCMS.BU.UE.Master
{
    public partial class MobileUser : System.Web.UI.MasterPage
    {
        B_User buser = new B_User();
        public M_UserInfo mu = new M_UserInfo();
        protected void Page_Init(object sender, EventArgs e)
        {
            string err = "";
            mu = buser.GetLogin();
            B_User.CheckIsLogged(Request.RawUrl);
            if (!B_User.CheckUserStatus(mu, ref err)) { function.WriteErrMsg(err); }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            //M_UserInfo mu = buser.GetLogin();
            //uName.Text = mu.UserName;
        }
    }
}