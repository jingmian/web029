using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Components;
using ZoomLa.Model;
using ZoomLa.Sns;

namespace ZoomLaCMS.BU.UE
{
    public partial class FGDraw : System.Web.UI.Page
    {
        B_User buser = new B_User();
        public M_UserInfo mu = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            mu = buser.GetLogin(false);
            if (!IsPostBack)
            {
                int count = Unbrealla.LendCountByUser(mu.UserID);
                Count_L.Text = count+ "把";
                double maxMoney = mu.SilverCoin - (count * PlugConfig.Info.Foregift);
                Money_L.Text = maxMoney.ToString("F2");
                Money_T.Text = maxMoney.ToString("F2");
                if (maxMoney < 1) { rd_tr.Visible = true; rd_div.InnerText = "没有可退的押金"; }
                else if (mu.Purse < 0) { rd_tr.Visible = true; rd_div.InnerText = "你的余额为负数,请先补齐余额"; }
                else { op_tr.Visible = true; }
            }
        }

        protected void Pay_Btn_Click(object sender, EventArgs e)
        {
            B_Cash cashBll = new B_Cash();
            double min = 29, multi = 29, max = 0;
            int count = Unbrealla.LendCountByUser(mu.UserID);
            max = mu.SilverCoin - (count * PlugConfig.Info.Foregift);
            double money = DataConverter.CDouble(Money_T.Text);
            //double fee = SiteConfig.UserConfig.WD_FeePrecent > 0 ? (money * (SiteConfig.UserConfig.WD_FeePrecent / 100)) : 0;
            //string shortPwd = Request.Form["ShortPwd_T"];
            if (money < 1) { function.WriteErrMsg("提现金额错误,不能小于1"); return; }
            if (money <min) { function.WriteErrMsg("提现金额必须>=[" + min.ToString("F0") + "]"); return; }
            if (money > max)
            {
                function.WriteErrMsg("提现金额必须小于[" + max + "]");
                return;
            }
            if ((money % multi) != 0)
            {
                function.WriteErrMsg("提现金额必须是[" + multi + "]的倍数");
                return;
            }
            //if (mu.Purse < (money + fee)) { function.WriteErrMsg("你只有[" + mu.Purse.ToString("f2") + "],需[" + (money + fee).ToString("F2") + "]才可提现"); return; }
            //if (string.IsNullOrEmpty(shortPwd)) { function.WriteErrMsg("未输入支付密码"); return; }
            //if (!mu.PayPassWord.Equals(StringHelper.MD5(shortPwd))) { function.WriteErrMsg("支付密码不正确"); return; }
            //生成提现单据
            M_Cash cashMod = new M_Cash();
            cashMod.UserID = mu.UserID;
            cashMod.money = money;
            cashMod.WithDrawFee = 0;
            cashMod.YName = mu.UserName;

            //cashMod.Account = bankMod.CardNum;
            //cashMod.Bank = Request.Form["Bank_T"];
            cashMod.PeopleName = mu.TrueName;
            cashMod.Remark = "押金提现";
            buser.MinusVMoney(mu.UserID, money, M_UserExpHis.SType.SIcon, cashMod.Remark);
            //if (cashMod.WithDrawFee > 0)
            //{
            //    buser.MinusVMoney(mu.UserID, cashMod.WithDrawFee, M_UserExpHis.SType.Purse, "提现手续费率" + SiteConfig.SiteOption.MastMoney.ToString("F2") + "%");
            //}
            cashBll.insert(cashMod);
            function.WriteSuccessMsg("押金提现申请成功,请等待管理员审核", "/User/");
        }
    }
}