using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using UIShell.OSGi;
using UIShell.OSGi.Configuration.BundleManifest;
using UIShell.OSGi.Core.Service;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.Plugin;

namespace ZoomLaCMS.Manage.AddOn.Plugins
{
    //路由无效则重新生成
    public partial class Default : CustomerPageAction
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                B_ARoleAuth.AuthCheckEx(ZLEnum.Auth.extend, "plugin");
                MyBind();
                Call.SetBreadCrumb(Master, "<li><a href='" + customPath2 + "Main.aspx'>工作台</a></li><li><a href='" + customPath2 + "Config/SiteInfo.aspx'>系统设置</a></li><li class='active'><a href='" + Request.RawUrl + "'>插件信息</a></li>");
            }
        }
        private void MyBind()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("url", typeof(string));
            dt.Columns.Add("state", typeof(string));
            dt.Columns.Add("symbol", typeof(string));
            dt.Columns.Add("vpath", typeof(string));
            dt.Columns.Add("desc", typeof(string));
            foreach (var item in Bootstrapper._registeredBunldeCache)
            {
                var bundle = BundleRuntime.Instance.Framework.GetBundleBySymbolicName(item.Key.SymbolicName);
                var bundleInfo = item.Key.BundleInfo;
                //item.Key.Activator.Policy=ActivatorPolicy.Lazy
                //item.Key.InitializedState = BundleInitializedState.Installed;
                DataRow dr = dt.NewRow();
                dr["name"] = bundle.Name;
                dr["url"] = bundleInfo.Category;
                dr["state"] = bundle.State.ToString();
                dr["symbol"] = bundle.SymbolicName;
                dr["vpath"] = function.PToV(item.Key.Path) + "/";
                dr["desc"] = bundleInfo.Description;
                dt.Rows.Add(dr);
            }
            RPT.DataSource = dt;
            RPT.DataBind();
        }
        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string name = e.CommandArgument.ToString();
            var bundle = BundleRuntime.Instance.Framework.GetBundleBySymbolicName(name);
            if (bundle == null) { Response.Redirect(Request.RawUrl); }
            switch (e.CommandName)
            {
                case "stop":
                    bundle.Stop(BundleStopOptions.General);
                    break;
                case "start":
                    bundle.Start(BundleStartOptions.General);
                    break;
            }
            Response.Redirect(Request.RawUrl);
        }
        //--------------
        public string GetState()
        {
            switch (Eval("state", ""))
            {
                case "Resolved":
                    return "<span style='color:red;'>已停止</span>";
                case "Installed":
                    return "<span style='color:red;'>未启用</span>";
                case "Active":
                    return "<span style='color:green;'>运行中</span>";
                default:
                    return Eval("State", "");
            }
        }
    }
}