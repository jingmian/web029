<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WithDraw.aspx.cs" Inherits="ZoomLaCMS.BU.UE.WithDraw" MasterPageFile="~/BU/UE/Master/MobileUser.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>充值提现</title>
<style>
.weui-label { font-weight:500;}
.weui-cell__bd p { margin-bottom:0;}
</style>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">

<div class="weui-cells">
<div class="weui-cell">
<div class="weui-cell__bd"><p>账户余额</p></div>
<div class="weui-cell__ft"><%=mu.Purse.ToString("F2") %></div>
</div>
<div class="weui-cell">
<div class="weui-cell__bd"><p>可退余额</p></div>
<div class="weui-cell__ft"><asp:Label runat="server" ID="Money_L"></asp:Label></div>
</div>
<div class="weui-cell">
<div class="weui-cell__hd"><label class="weui-label">提取余额</label></div>
<div class="weui-cell__bd">
<asp:TextBox runat="server" ID="Money_T" Text="29.00" CssClass="weui-input" placeholder="请输入需要提取的金额" />
</div>
</div>
</div>

<div class="weui-btn-area" runat="server" id="op_tr" visible="false">
<asp:Button runat="server" ID="Pay_Btn" class="weui-btn" Text="提取余额" OnClick="Pay_Btn_Click" style="background:#ee7218; color:#fff;" />
</div>

<div runat="server" id="rd_tr" visible="false">
<div class="alert alert-danger" id="rd_div" runat="server"></div>
</div>
<div class="alert alert-info">
    <div>1,请确保已关注公众号</div>
    <div>2,资金将直接进入微信零钱</div>
    <div>3,余额必须保持在9元以上</div>
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script>
$("#Money_T").focus();
</script>
</asp:Content>