<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MyLiveLend.aspx.cs" Inherits="ZoomLaCMS.BU.UE.Store.MyLiveLend" MasterPageFile="~/Common/Master/Empty.master"%>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>我的雨伞</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<div class="weui-panel weui-panel_access">
<div class="weui-panel__hd">我的伞列表</div>
<div class="weui-panel__bd">
<asp:Repeater runat="server" ID="RPT" OnItemCommand="RPT_ItemCommand">
<ItemTemplate>
<a href="javascript:void(0);" class="weui-media-box weui-media-box_appmsg" onClick="showLend(<%#Eval("ID")%>);">
<div class="weui-media-box__bd">
<h4 class="weui-media-box__title">ID：<%#Eval("ID") %></h4>
<p class="weui-media-box__desc">伞号：<%#Eval("Balance_remark") %></p>
<p class="weui-media-box__desc">借出时间：<%#Eval("AddTime","{0:yyyy年MM月dd日 HH:mm}") %></p>
<p>借出天数：<%#(DateTime.Now-Convert.ToDateTime(Eval("AddTime"))).TotalDays.ToString("F0") %></p>
</div>
</a>
</ItemTemplate>
</asp:Repeater>
</div>
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script"></asp:Content>