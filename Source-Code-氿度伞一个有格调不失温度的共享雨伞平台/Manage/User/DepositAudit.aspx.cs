using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;

namespace ZoomLaCMS.Manage.User
{
    public partial class DepositAudit : System.Web.UI.Page
    {
        B_Cash cashBll = new B_Cash();
        B_User buser=new B_User();
        //dealed|reject|view
        public string Action { get { return (Request.QueryString["action"] ?? "reject").ToLower(); } }
        public int Mid { get { return DataConverter.CLng(Request.QueryString["id"]); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Mid < 1) { function.WriteErrMsg("未指定提现申请ID"); }
                M_Cash cashMod = cashBll.SelReturnModel(Mid);
                if (cashMod == null) { function.WriteErrMsg("指定的提现申请不存在"); }
                M_UserInfo mu = buser.SelReturnModel(cashMod.UserID);
                UName_L.Text = cashMod.YName;
                Money_L.Text=cashMod.money.ToString("F2");
                BackDecs_T.Text = cashMod.Result;
                Attach_Hid.Value = cashMod.ResultImg;
                switch (Action)
                {
                    case "dealed":
                        Accept_Btn.Visible = true;
                        break;
                    case "view":
                        optr.Visible = false;
                        upfile_btn.Visible = false;
                        break;
                    default:
                        Reject_Btn.Visible = true;
                        break;
                }
                Call.HideBread(Master);
            }
        }
        //拒绝提现,返还金额
        protected void Reject_Btn_Click(object sender, EventArgs e)
        {
            M_Cash cashMod = FillCashModel();
            cashMod.yState = (int)ZLEnum.WDState.Rejected;
            cashBll.UpdateByID(cashMod);
            switch (cashMod.Classes)
            {
                case 0:
                    buser.AddMoney(cashMod.UserID, (cashMod.money + cashMod.WithDrawFee), M_UserExpHis.SType.SIcon, "拒绝提现,返还预扣押金");
                    break;
                case 1:
                    buser.AddMoney(cashMod.UserID, (cashMod.money + cashMod.WithDrawFee), M_UserExpHis.SType.Purse, "拒绝提现,返还预扣余额");
                    break;
                default:
                    throw new Exception("申请单类型不正确");
            }
            function.Script(this, "parent.diag.CloseModal();parent.location.reload();");
        }
        //确认提现,上传打款凭证等
        protected void Accept_Btn_Click(object sender, EventArgs e)
        {
            M_Cash cashMod = FillCashModel();
            cashMod.yState = (int)ZLEnum.WDState.Dealed;
            cashBll.UpdateByID(cashMod);
            function.Script(this, "parent.diag.CloseModal();parent.location.reload();");
        }
        private M_Cash FillCashModel()
        {
            M_AdminInfo adminMod = B_Admin.GetLogin();
            M_Cash cashMod = cashBll.SelReturnModel(Mid);
            cashMod.Result = BackDecs_T.Text;
            cashMod.ResultImg = Attach_Hid.Value;
            cashMod.AdminID = adminMod.AdminId;
            if (cashMod.AdminID < 1) { throw new Exception("管理员ID不正确"); }
            return cashMod;
        }
    }
}