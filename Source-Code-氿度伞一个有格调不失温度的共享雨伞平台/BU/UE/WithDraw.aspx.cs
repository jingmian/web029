using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.BLL.User.Addon;
using ZoomLa.BLL.WxPayAPI;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.Model.User;

namespace ZoomLaCMS.BU.UE
{
    public partial class WithDraw : System.Web.UI.Page
    {
        B_UserAPP appBll = new B_UserAPP();
        B_User buser = new B_User();
        public M_UserInfo mu = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            mu = buser.GetLogin(false);
            if (!IsPostBack)
            {
                double maxMoney = GetMaxMoney();
                Money_L.Text = maxMoney.ToString("F2");
                Money_T.Text = maxMoney.ToString("F2");
                if (maxMoney < 1) { rd_tr.Visible = true; rd_div.InnerText = "没有可提现的余额"; }
                else if (mu.Purse < 0) { rd_tr.Visible = true; rd_div.InnerText = "你的余额为负数,不可提现"; }
                else { op_tr.Visible = true; }
            }
        }

        protected void Pay_Btn_Click(object sender, EventArgs e)
        {
            function.WriteErrMsg("提现功能暂未开放");
            double max = GetMaxMoney();
            if (max < 1) { function.WriteErrMsg("可提现余额不够"); return; }
            WxAPI wxapi = WxAPI.Code_Get();
            M_UserAPP appMod = appBll.SelReturnModel(mu.UserID);
            if (appMod == null || string.IsNullOrEmpty(appMod.OpenID)) { function.WriteErrMsg("未绑定微信账号"); return; }
            CommonReturn ret = SendMoney(wxapi.AppId, appMod.OpenID, max, "余额提现");
            if (ret.isok)
            {
                buser.MinusVMoney(mu.UserID, max, M_UserExpHis.SType.Purse, "余额提现");
                function.WriteSuccessMsg("提现成功","/User/");
            }
            else
            {
                function.WriteErrMsg("操作失败,原因:"+ret.err);
            }
            //var cert= HttpService.Cert_GetByName(wxapi.AppId.Pay_SSLPath);
            //CommonReturn ret = SendMoney(wxapi.AppId, "oGUkXxN83jNbKCbtrNFny5tqHtok", 1, "测试付款");
        }
        private double GetMaxMoney()
        {
            double min = 9;
            if (mu.Purse <= min) { return 0; }
            else { return mu.Purse - min; }
        }
        private CommonReturn SendMoney(M_WX_APPID appMod, string openid, double amount, string desc)
        {
            WxPayData wxdata = new WxPayData();
            wxdata.SetValue("partner_trade_no", B_OrderList.CreateOrderNo(M_OrderList.OrderEnum.Normal));
            wxdata.SetValue("openid", openid);
            wxdata.SetValue("check_name", "NO_CHECK");//OPTION_CHECK NO_CHECK
            //wxdata.SetValue("re_user_name", trueName.Trim());
            wxdata.SetValue("amount", (int)amount * 100);
            wxdata.SetValue("desc", desc);
            wxdata.SetValue("spbill_create_ip", Request.ServerVariables["LOCAl_ADDR"]);//必须填写
            WxPayData result = WxPayApi.Pay_Transfer(wxdata, appMod);
            if (result.GetValue("result_code").ToString().ToUpper().Equals("SUCCESS"))
            {
                return CommonReturn.Success();
            }
            else
            {
                string err = result.GetValue("return_msg").ToString();
                return CommonReturn.Failed(err);
            }
        }
    }
}