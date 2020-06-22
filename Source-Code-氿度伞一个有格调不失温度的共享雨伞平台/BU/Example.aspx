<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Example.aspx.cs" Inherits="ZoomLaCMS.Tools.Example" MasterPageFile="~/Common/Master/Empty.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
    <input type="button" value="我要借伞" onclick="ueclient.lend();" />
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script src="/JS/Modal/APIResult.js"></script>
<script>
    var ueclient = { url: "/BU/Client.ashx?action=" };
    ueclient.lend = function () {
        var ref = this;
        $.post(ref.url + "CheckMoney", {}, function (data) {
            var model = APIResult.getModel(data);
            if (APIResult.isok(model)) {
                //满足条件,申请借伞
                $.post(ref.url + "ue_lend", { storeID: 1 }, function (data) {
                    model = APIResult.getModel(data);
                    if (APIResult.isok(model)) {
                        //借伞成功,显示成功标识
                        alert("借伞成功,订单ID="+model.result);
                    }
                    else { alert(model.retmsg); }
                })
            }
            else//前往充值
            {
                console.log(model.retmsg);
                location = "/BU/UE/CheckAndCharge.aspx";
            }
        })
    }
    var uestore = { url: "/BU/Store.ashx?action=" };
    
</script>
</asp:Content>