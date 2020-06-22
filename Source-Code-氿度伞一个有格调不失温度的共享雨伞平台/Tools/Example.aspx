<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Example.aspx.cs" Inherits="ZoomLaCMS.Tools.Example" MasterPageFile="~/Common/Master/Empty.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">

</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script src="/JS/Mobile/vconsole.min.js"></script>
<script src="/JS/Modal/APIResult.js"></script>
<script>
    //$.post("/BU/Client.ashx?action=isfollow", {}, function (data) {
    //    var model = APIResult.getModel(data);
    //    if (!APIResult.isok(model)) {
    //        console.log("未关注");
    //    }
    //})
    $.post("/BU/Client.ashx?action=isfollow").then((data) => {
        var model = APIResult.getModel(data);
        if (!APIResult.isok(model)) {
            console.log("未关注");
        }
        else { console.log("已经关注了"); }
    })
</script>
</asp:Content>