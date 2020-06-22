<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReturnList.aspx.cs" Inherits="ZoomLaCMS.BU.UE.Store.ReturnList" MasterPageFile="~/Common/Master/Empty.master"%>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>还伞确认</title>
<style>
body { background:#f8f8f8;}
.order-item { font-family:"STHeiti","Microsoft YaHei","黑体","arial";}
.order-item { background:#fff; margin-top:5px; border-bottom:1px solid #e3e5e9;}
.gray9{color:#999;}
.tips_div { padding-top:10px; padding-bottom:10px; padding-left:15px; padding-right:15px; border-top:1px solid #e3e5e9; border-bottom:1px solid #e3e5e9; position:relative;}
.prolist { padding:5px 10px;}
.prolist .media p { margin-bottom:0; font-size:0.9em;}
.prolist .media p span { margin-right:5px;}
</style>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<div class="weui-cells__title">还伞列表</div>
<asp:Repeater runat="server" ID="RPT" OnItemCommand="RPT_ItemCommand">
<ItemTemplate>
<div class="order-item">
<div class="tips_div">
<div><span class="gray9">ID：<%#Eval("ID") %> | 伞号：<%#Eval("Balance_remark") %> | 借出点：<%#Eval("StoreName") %></span></div>
<div class="orderinfo"><span class="gray9">借出时间：<%#Eval("AddTime","{0:yyyy年MM月dd日 HH:mm}") %></span></div>
<div class="shopinfo"><span class="gray9">借出天数：<%#(DateTime.Now-Convert.ToDateTime(Eval("AddTime"))).TotalDays.ToString("F0") %></span></div>
<%#ShowRepairInfo() %>
</div>
<div class="prolist">
<p class="text-right" style="margin-bottom:0;">
    <asp:LinkButton runat="server" CommandName="repair" CommandArgument='<%#Eval("ID") %>' class="btn" Style="background: #ee7218; color: #fff;" Visible='<%#Eval("OrderStatus","")=="-2" %>'>坏伞回收</asp:LinkButton>
    <asp:LinkButton runat="server" CommandName="cancel_repair" CommandArgument='<%#Eval("ID") %>' class="btn" Style="background: #ee7218; color: #fff;" Visible='<%#Eval("OrderStatus","")=="-2" %>'
        OnClientClick="return confirm('确定要取消报修吗,预扣余额将返还用户');">取消报修</asp:LinkButton>
<asp:LinkButton runat="server" CommandName="return" CommandArgument='<%#Eval("ID") %>' class="btn" style="background:#ee7218; color:#fff;" Visible='<%#Eval("OrderStatus","")=="-1" %>'>确认归还</asp:LinkButton>
<asp:LinkButton runat="server" CommandName="cancel" CommandArgument='<%#Eval("ID") %>' class="btn" style="background:#ee7218; color:#fff;" Visible='<%#Eval("OrderStatus","")=="-1" %>'>取消归还</asp:LinkButton>
</p>
</div>
</div>
</ItemTemplate>
</asp:Repeater>

<%Call.Label("{ZL.Label id=\"微信底部\"/}");%>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script>
$("#foot_user").addClass("weui-bar__item_on");
</script>
<script runat="server">
    public string ShowRepairInfo()
    {
        if (ZoomLa.Common.DataConverter.CLng(Eval("IsCount")) > 0)
        {
            return "<div class='orderinfo'><span class='gray9'>"+Eval("Internalrecords")+"</span></div>";
        }
        else { return ""; }
    }
</script>
</asp:Content>