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
using ZoomLa.Model;
using ZoomLa.Common;
using System.Text;
using ZoomLa.SQLDAL.SQL;
using ZoomLa.SQLDAL;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ZoomLaCMS.Manage.Pay
{
    public partial class PaymentList : CustomerPageAction
    {
        B_Payment bll = new B_Payment();
        public int UserID { get { return DataConverter.CLng(Request.QueryString["UserID"]); } }
        public int Status { get { return DataConverter.CLng(Request.QueryString["Status"],-100); } }
        public string Remark { get { return Request.QueryString["remark"] ?? ""; } }
        public string ParamUrl
        {
            get
            {
                return "PaymentList.aspx?OrderType=" + DataConvert.CLng(Request["OrderType"], -100) + "&";
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            B_ARoleAuth.AuthCheckEx(ZLEnum.Auth.system, "pay");
            if (!IsPostBack)
            {
                MyBind();
            }
        }
        public void MyBind(string orderwhere = "PaymentID DESC")
        {
            if (Status == (int)M_OrderList.StatusEnum.Recycle)
            {
                SwitchUrl_A.HRef = customPath2 + "Pay/PayMentList.aspx";
                SwitchUrl_A.InnerText = "返回列表";
                normal_div.Visible = false;
                recycle_div.Visible = true;
            }
            else
            {
                SwitchUrl_A.HRef = customPath2 + "Pay/PayMentList.aspx?Status=-99";
                SwitchUrl_A.InnerText = "回收站";
                normal_div.Visible = true;
                recycle_div.Visible = false;
            }
            DataTable dt = SelByWhere();
            dt.DefaultView.Sort = orderwhere;
            this.RPT.DataSource = dt;
            this.RPT.DataBind();
        }
        public DataTable SelByWhere()
        {
            DataTable dt = SelPage(1, 50000, UserID,DataConvert.CLng(Request["OrderType"],-100),
                DataConverter.CDouble(MinMoney_T.Text), DataConverter.CDouble(MaxMoney_T.Text),
                Search_Drop.SelectedValue, Search_T.Text, Status, Remark).dt;
            return dt;
        }
        public string GetStatus(string status)
        {
            if (status == "3")
                return "<span style='color:green;' class='fa fa-check'></span>";
            else
                return "<span style='color:red;' class='fa fa-remove'></span>";

        }
        public string GetCStatus(string cstatus)
        {
            if (DataConverter.CBool(cstatus))
                return "<span style='color:green;' class='fa fa-check'></span>";
            else
                return "<span style='color:red;' class='fa fa-remove'></span>";

        }

        protected void DropDownList1_SelectedIndexChanged1(object sender, EventArgs e)
        {
            MyBind();
        }
        private string ClearAscStr(string text, out bool isasc)
        {
            IDAsc.Text = "ID";
            PriceAsc.Text = "金额";
            DateAsc.Text = "完成时间";
            isasc = text.Contains("fa fa-arrow-down");
            return "<span class='" + (isasc ? "fa fa-arrow-up" : "fa fa-arrow-down") + "'></span>";
        }
        protected void IDAsc_Click(object sender, EventArgs e)
        {
            bool isasc = true;
            string str = ClearAscStr(IDAsc.Text, out isasc);
            IDAsc.Text += str;
            MyBind("PaymentID " + (isasc ? "DESC" : "ASC"));
        }

        protected void DateAsc_Click(object sender, EventArgs e)
        {
            bool isasc = true;
            string str = ClearAscStr(DateAsc.Text, out isasc);
            DateAsc.Text += str;
            MyBind("SuccessTime " + (isasc ? "DESC" : "ASC"));
        }

        protected void PriceAsc_Click(object sender, EventArgs e)
        {
            bool isasc = true;
            string str = ClearAscStr(PriceAsc.Text, out isasc);
            PriceAsc.Text += str;
            MyBind("MoneyPay " + (isasc ? "DESC" : "ASC"));
        }

        protected void Serarch_Btn_Click(object sender, EventArgs e)
        {
            MyBind();
        }

        protected void SearchMoney_Btn_Click(object sender, EventArgs e)
        {
            MyBind();
        }
        protected void RPT_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id=Convert.ToInt32(e.CommandArgument);
            switch (e.CommandName.ToLower())
            {
                case "del":
                    bll.ChangeRecycle(id.ToString(),1);
                    break;
                case "recdel":
                    bll.RealDelByIDS(e.CommandArgument.ToString());
                    break;
                case "rec":
                    bll.ChangeRecycle(id.ToString(),0);
                    break;
            }
            MyBind();
        }
        protected void ExportExcel_Btn_Click(object sender, EventArgs e)
        {
            DataTable dt = SelByWhere();
            dt.Columns.Add("CStatusStr");
            dt.Columns.Add("StatusStr");
            foreach (DataRow dr in dt.Rows)
            {
                bool cstatus = DataConverter.CBool(dr["cstatus"].ToString());
                dr["CStatusStr"] = cstatus ? "已处理" : "未处理";
                int status = DataConverter.CLng(dr["status"]);
                dr["StatusStr"] = status == 3 ? "已结束" : "未完成";
            }
            DataTable newDt = dt.DefaultView.ToTable(false, "PaymentID,UserName,PaymentNum,PayPlatName,MoneyPay,MoneyTrue,StatusStr,CStatusStr,SuccessTime".Split(','));
            string columnames = "ID,会员名,订单号,支付平台,金额,实际金额,交易状态,处理结果,完成时间";
            SafeSC.DownStr(OfficeHelper.ExportExcel(newDt, columnames), DateTime.Now.ToString("yyyyMMdd") + "充值信息表.xls");
        }
        protected void Dels_Btn_Click(object sender, EventArgs e)
        {
            string ids = Request.Form["idchk"];
            if (!string.IsNullOrEmpty(ids))
            {
                bll.ChangeRecycle(ids,1);
            }
            MyBind();
        }
        protected void Recover_Btn_Click(object sender, EventArgs e)
        {
            string ids = Request.Form["idchk"];
            if (!string.IsNullOrEmpty(ids))
            {
                bll.ChangeRecycle(ids,0);
                function.WriteSuccessMsg("还原成功");
            }
        }
        //彻底删除
        protected void RealDel_Btn_Click(object sender, EventArgs e)
        {
            string ids = Request.Form["idchk"];
            if (!string.IsNullOrEmpty(ids))
            {
                bll.RealDelByIDS(ids);
                function.WriteSuccessMsg("删除成功");
            }
        }
        protected void ClearRecycle_Btn_Click(object sender, EventArgs e)
        {
            bll.ClearRecycle();
            function.WriteSuccessMsg("回收站已经清空");
        }

        /// <summary>
        /// 用于充值信息列表页
        /// </summary>
        /// <param name="orderType">订单类型,-1全部</param>
        /// <param name="minMoney">金额必须大于</param>
        /// <param name="maxMoney">金额必须小于</param>
        /// <param name="type">搜索类型</param>
        /// <param name="skey">订单号模糊查询</param>
        /// <returns></returns>
        public PageSetting SelPage(int cpage, int psize, int uid = 0, int orderType = -1, double minMoney = 0, double maxMoney = 0, string type = "", string skey = "", int status = 0, string sysremark = "")
        {
            List<SqlParameter> sp = new List<SqlParameter>() { new SqlParameter("skey", "%" + skey + "%"), new SqlParameter("sysremark", "%" + sysremark + "%") };
            string mtable = "(SELECT A.*,B.UserName,B.[PERMISSIONS],B.HoneyName FROM (SELECT A.*,B.PayPlatName FROM ZL_Payment A LEFT JOIN ZL_PayPlat B ON A.PayPlatID=B.PayPlatID) A LEFT JOIN ZL_User B ON A.UserID=B.UserID)";
            string where = " 1=1 ";
            if (uid > 0) { where += " AND A.UserID=" + uid; }
            if (orderType > -1) { where += " AND OrderType=" + orderType; }
            if (minMoney >= 0) { where += " AND A.MoneyPay>=" + minMoney; }
            if (maxMoney > 0) { where += " AND A.MoneyPay<=" + maxMoney; }
            if (!string.IsNullOrEmpty(skey))
            {
                switch (type)
                {
                    case "1":
                        where += " AND UserName LIKE @skey";
                        break;
                    case "2":
                        where += " AND PaymentNum LIKE @skey";
                        break;
                    case "3":
                        where += " AND PayNo LIKE @skey";
                        break;
                    case "4":
                        where += " AND A.UserID = " + DataConverter.CLng(skey); ;
                        break;
                    default:
                        break;
                }
            }
            //回收站
            if (status == (int)M_OrderList.StatusEnum.Recycle) { where += " AND A.[IsDel]=1"; }
            else {
                where += " AND (A.[IsDel]!=1 OR A.IsDel IS NULL)";
                if (status != -100) { where += " AND A.[Status] IN (" + status + ")"; }
                
            }
            if (!string.IsNullOrEmpty(sysremark)) { where += " AND SysRemark LIKE @sysremark"; }
            //string sql = "(SELECT A.*,B.UserName FROM (SELECT A.*,B.PayPlatName FROM " + TbName + " A LEFT JOIN ZL_PayPlat B ON A.PayPlatID=B.PayPlatID) A ";
            //sql += "LEFT JOIN ZL_User B ON A.UserID=B.UserID)";
            //return SqlHelper.JoinQuery("A.*,B.OrderType", sql, "ZL_OrderInfo", "A.PaymentNum=B.OrderNo", where, "", sp);
            PageSetting setting = PageSetting.Double(cpage, psize, mtable, "ZL_OrderInfo", "A.PaymentID", "A.PaymentNum=B.OrderNo", where, "", sp, "A.*,B.OrderType");
            DBCenter.SelPage(setting);
            return setting;
        }
    }
}