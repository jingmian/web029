<%@ Page Language="C#" AutoEventWireup="true" CodeFile="CheckAndCharge.aspx.cs" Inherits="ZoomLaCMS.BU.UE.CheckAndCharge" MasterPageFile="~/Common/Master/Empty.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>充值</title>
<style>
.wei-footer .weui-tabbar__item.weui-bar__item_on .weui-tabbar__icon,.wei-footer .weui-tabbar__item.weui-bar__item_on .weui-tabbar__icon>i,.wei-footer .weui-tabbar__item.weui-bar__item_on .weui-tabbar__label { color:#ee7218;}
.weui-cell__bd p { margin-bottom:0;}
label { font-weight:500; margin-bottom:1em; display:block; height:3em; line-height:3em; text-align:center; border:1px solid #ee7218; border-radius:0.3em;}
label.active { background:#ee7218; color:#FFF;}
label input[type="radio"] { display:none;}
</style>
<link href="/Plugins/Third/alert/sweetalert.css" rel="stylesheet" />
<script src="/Plugins/Third/alert/sweetalert.min.js?v=1"></script>
<script src="/JS/Modal/APIResult.js"></script>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">

<div class="container">

<div class="weui-cells__title">余额充值</div>
<div class="row">
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6" runat="server" id="zero_wrap" visible="false">
<label>
<input type="radio" value="0" name="charge_rad"/>
不充值
</label>
</div>
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6">
<label>
<input type="radio" value="9" name="charge_rad" checked="checked"/>
9 元
</label>
</div>
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6">
<label>
<input type="radio" value="19" name="charge_rad"/>
19 元
</label>
</div>
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6">
<label>
<input type="radio" value="50" name="charge_rad"/>
50 元
</label>
</div>
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6">
<label>
<input type="radio" value="100" name="charge_rad"/>
100 元
</label>
</div>
<div class="col-lg-6 col-md-6 col-sm-6 col-xs-6">
<label>
<input type="radio" value="1" name="charge_rad"/>
1 元捐赠通道(测试)
</label>
</div>
</div>
<div class="weui-btn-area">
<asp:Button runat="server" ID="Pay_Btn" Text="前往支付" class="weui-btn hidden" OnClick="Pay_Btn_Click" style="background:#ee7218; color:#fff;" />
<input class="weui-btn" type="button" style="background:#ee7218; color:#fff;" onClick="PayCheck();" value="前往支付" />
</div>
</div>
<%Call.Label("{ZL.Label id=\"微信底部\"/}");%>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script>
$(function () {
    $("input[value='9']").click();
    $("input[type='radio']:checked").parent().addClass("active");
    $("#foot_user").addClass("weui-bar__item_on");
    $("label input").change(function(){
        $(":checked").parent().addClass("active").parent().siblings("div").find("label").removeClass("active");
    });
})


function PayCheck(){
    $.post("/BU/Client.ashx?action=CheckMoney", {}, function (data) {
        var model = APIResult.getModel(data);
        console.log(model);
        if(model.retcode==1){
            $("#Pay_Btn").click();
        }else{
            swal({
              title: "押金不足",
              text: "请点击确认前往交押金",
              type: "warning",
              showCancelButton: true,
              confirmButtonColor: "#ee7218",
              confirmButtonText: "前往充值",
              closeOnConfirm: false
            },function(){
              window.location.href="/BU/UE/Foregift.aspx";
            });
        }
    });
}
</script>
</asp:Content>