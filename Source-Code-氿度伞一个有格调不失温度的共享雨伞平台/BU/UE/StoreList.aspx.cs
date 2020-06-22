using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.SQLDAL;

namespace ZoomLaCMS.BU.UE
{
    public partial class StoreList : System.Web.UI.Page
    {
        public int NodeID { get { return DataConvert.CLng(Request["NodeID"],19); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            //19
            if (!IsPostBack)
            {
                Data_Hid.Value = JsonConvert.SerializeObject(Sel());
            }
        }
        private DataTable Sel()
        {
            //(A.NodeID=" + NodeID + " OR A.FirstNodeID=" + NodeID + ") and
            string fields = "A.GeneralID,A.CreateTime,A.NodeID,A.Title,B.addr,B.content,B.logo,B.lxr,B.synopsis,B.map";
            string where = " A.Status=99 and A.ModelID=24";
            DataTable dt = DBCenter.JoinQuery(fields, "ZL_CommonModel", "ZL_Store_Reg", "A.ItemID=B.ID", where, "A.GeneralID DESC");
            return dt;
        }
    }
}