<%@ Page Language="C#" AutoEventWireup="true" CodeFile="OrderList.aspx.cs" Inherits="ZoomLaCMS.BU.UE.Admin.OrderList" MasterPageFile="~/Manage/I/Default.Master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>订单管理</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<ol id="BreadNav" class="breadcrumb navbar-fixed-top">
        <li><a href="/Admin/Main.aspx">工作台</a></li>
        <li><a href="<%=Request.RawUrl %>">订单管理</a></li>
    </ol>
<div class="input-group" style="width:682px;">
    <asp:DropDownList runat="server" ID="Type_DP" class="form-control text_md">
        <asp:ListItem Value="all" Selected="True">全部</asp:ListItem>
        <asp:ListItem Value="store">店家借出</asp:ListItem>
        <asp:ListItem Value="user">用户转借</asp:ListItem>
    </asp:DropDownList>
    <asp:DropDownList runat="server" ID="Status_DP" class="form-control text_md">
        <asp:ListItem Selected="True" Value="0">出借中</asp:ListItem>
        <asp:ListItem Value="-1">归还(待确认)</asp:ListItem>
        <asp:ListItem Value="99">已完结</asp:ListItem>
        <asp:ListItem Value="-100">全部</asp:ListItem>
    </asp:DropDownList>
    <asp:TextBox runat="server" ID="UName_T" class="form-control text_md" placeholder="请输入用户名" />
    <span class="input-group-btn">
        <asp:Button runat="server" ID="Skey_Btn" Text="筛选订单" class="btn btn-info" OnClick="Skey_Btn_Click" />
    </span>
</div>
<div class="margin_t10">
<table class="table table-bordered table-stipred">
    <thead><tr>
        <td class="td_s">ID</td>
        <td class="td_m">伞号</td>
        <td class="td_m">状态</td>
        <td>出借点</td>
        <td class="td_l">用户名</td>
        <td class="td_l">出借时间</td>
        <td class="td_m">天数</td>
       <!-- <td class="td_m">已扣费用</td>-->
        <td class="td_l">归还时间</td>
        <td class="td_l">归还点</td>
        <td class="td_l">备注</td>
           </tr></thead>
    <ZL:Repeater runat="server" ID="RPT" OnItemCommand="RPT_ItemCommand"  PageSize="20" PagePre="<tr><td><label class='allchk_l'><input type='checkbox' id='chkAll'/>全选</label></td><td colspan='12'><div class='text-center'>"
     PageEnd="</div></td></tr>">
        <ItemTemplate>
            <tr>
                <td><%#Eval("ID") %></td>
                <td><%#Eval("Balance_Remark") %></td>
                <td><%#ZoomLa.Sns.Unbrealla.T_GetStatus(Eval("OrderStatus")) %></td>
                <td><%#Eval("StoreName") %></td>
                <td><a href="javascript:;" onclick="user.showuinfo(<%#Eval("UserID") %>)">
                    <%#ZoomLa.BLL.B_User.GetUserName(Eval("Permissions",""),Eval("HoneyName","")) %>(<%#Eval("UserID") %>)

                    </a></td>
                <td><%#Eval("AddTime","{0:yyyy/MM/dd HH:mm}") %></td>
               <!-- <td><%#ZoomLa.Sns.Unbrealla.T_CalcDays(Eval("AddTime")) %></td>-->
                <td><%#Eval("OrdersAmount","{0:F2}") %></td>
                <td><%#Eval("ExpTime") %></td>
                <td><%#Eval("RStoreName") %></td>
                <td><%#Eval("OrderMessage") %></td>
            </tr>
        </ItemTemplate>
        <FooterTemplate></FooterTemplate>
    </ZL:Repeater>
</table>
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="ScriptContent">
    <script src="/JS/Controls/ZL_Dialog.js"></script>
</asp:Content>