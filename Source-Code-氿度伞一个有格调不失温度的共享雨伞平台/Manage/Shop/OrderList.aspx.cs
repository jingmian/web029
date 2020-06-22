using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Visitors;
using ZoomLa.BLL;
using ZoomLa.BLL.Helper;
using ZoomLa.BLL.Shop;
using ZoomLa.Common;
using ZoomLa.Components;
using ZoomLa.Model;
using ZoomLa.SQLDAL;
using ZoomLa.SQLDAL.SQL;

namespace ZoomLaCMS.Manage.Shop
{
    public partial class OrderList : CustomerPageAction
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("/BU/UE/Admin/OrderList.aspx");
        }
    }
}