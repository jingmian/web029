<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ForeGift.aspx.cs" Inherits="ZoomLaCMS.BU.UE.ForeGift" MasterPageFile="~/BU/UE/Master/MobileUser.master"%>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>押金充值</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<div class="container padding0_xs">
    <div class="weui-cells__title">充值中心</div>
    <div class="weui-cells">
        <div class="weui-cell">
            <div class="weui-cell__bd">
                <p>押金</p>
            </div>
            <div class="weui-cell__ft">需充值
                <asp:Label runat="server" ID="Foregift_L" Text="0"></asp:Label>
                元</div>
        </div>
    </div>
</div>
<div>
    
</div>
<div class="weui-btn-area">
<asp:Button runat="server" ID="Charge_Btn" Text="前往充值" class="weui-btn" OnClick="Charge_Btn_Click" style="background:#ee7218; color:#fff;" />
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script"></asp:Content>