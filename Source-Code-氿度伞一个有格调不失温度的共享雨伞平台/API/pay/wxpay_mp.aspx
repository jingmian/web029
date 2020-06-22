<%@ Page Language="C#" AutoEventWireup="true" CodeFile="wxpay_mp.aspx.cs" Inherits="ZoomLaCMS.API.pay.wxpay_mp" MasterPageFile="~/Common/Master/Empty.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>公众号支付</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<%--<div class="text-center" style="margin-top: 40%;">
    <a href="https://open.weixin.qq.com/connect/oauth2/authorize?appid=<%:AppID %>&redirect_uri=<%:siteurl %>&response_type=code&scope=snsapi_userinfo&state=STATE#wechat_redirectx" class="btn btn-danger">发起支付</a>
    <asp:Label runat="server" ID="Remind_L" ForeColor="Red"></asp:Label>
    <asp:Label runat="server" ID="Client_L" ForeColor="Red"></asp:Label>
</div>--%>
<asp:HiddenField runat="server" ID="LendInfo_Hid" Value="" />
<div class="container">
<div class="weui-cells__title">支付类型</div>
<div runat="server" id="Lend1" visible="false">
<div class="well">押金支付</div>
<div>
温馨提示：<br/>
24小时内免费用伞<br/>
还伞、转借伞后押金申请退还
</div>
<br/>
</div>
<div runat="server" id="Lend2" visible="false">
<div class="well">余额充值</div>
<div>
温馨提示：<br/>
24小时内免费用伞<br/>
超过24小时，一天扣费1元
</div>
<br/>
</div>
<div class="alert alert-info" role="alert">请稍等，系统正在处理您的支付请求...</div>
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script src="/JS/Modal/APIResult.js"></script>
<script src="/JS/Controls/ZL_Dialog.js"></script>
<!--<script src="/JS/Mobile/vconsole.min.js"></script>-->
<script>
    //a 97,n 110,p 112,s 115,t 116 
    var ueclient = { url: "/BU/Client.ashx?action=", interal: null };
    ueclient.lend = function () {
        var lendInfo = JSON.parse($("#LendInfo_Hid").val());
        $.post("/BU/Client.ashx?action=" + "ue_lend", lendInfo, function (data) {
            var model = APIResult.getModel(data);
            if (APIResult.isok(model)) {
                location = "/BU/UE/MyLend.aspx";
            }
            else {
                switch (model.result.type) {
                    case 1:
                    case 2:
                        location = "/PayOnline/wxpay_mp.aspx?payno=" + model.result.payno;
                        break;
                    default:
                        alert(data);
                        break;
                }
            }
        })
    }
    //接收回调完成充值扣再调用lend
    ueclient.payCheck = function () {
        var ref = this;
        var payno = "<%=PayNo%>";
        ref.interal = setInterval(function () {
            $.post(ref.url + "payisok", { "payno": payno }, function (data) {
                var model = APIResult.getModel(data);
                if (APIResult.isok(model)) {
                    clearInterval(ref.interal);
                    setTimeout(ref.lend, 1000);
                }
            })
        }, 1500);
    }
    function onBridgeReady() {
        WeixinJSBridge.invoke(
            'getBrandWCPayRequest', {
                "appId": "<%=appMod.APPID%>",     //公众号名称，由商户传入 
                "nonceStr": "<%:ZoomLa.BLL.WxAPI.nonce%>", //随机串  
                "package": "prepay_id=<%=prepay_id%>",
                "signType": "MD5",         //微信签名方式： 
                "timeStamp": "<%=timestr%>", //时间戳，自1970年以来的秒数     
                "paySign": "<%=paySign%>"//微信签名 
            },
            function (res) {
                //$("#Client_L").text(JSON.stringify(res));
                if (res.err_msg == "get_brand_wcpay_request:ok") {
                    //检测订单类型,如果是押金进入对应界面
                    var json = $("#LendInfo_Hid").val();
                    if (json != "") {
                        var lendInfo = JSON.parse($("#LendInfo_Hid").val());
                        switch (lendInfo.action) {
                            case "ue_lend":
                                ueclient.payCheck();
                                break;
                            case "ue_lenttouser":
                                setTimeout(function () { location = "/BU/UE/LentToUser.aspx?oid=" + lendInfo.oid; },1000);
                                break;

                        }
                    } else { location.href = "<%=SuccessUrl %>";}
                }
               
            }
        );
    }
    if (typeof WeixinJSBridge == "undefined") {
        if (document.addEventListener) {
            document.addEventListener('WeixinJSBridgeReady', onBridgeReady, false);
        } else if (document.attachEvent) {
            document.attachEvent('WeixinJSBridgeReady', onBridgeReady);
            document.attachEvent('onWeixinJSBridgeReady', onBridgeReady);
        }
    } else {
        onBridgeReady();
    }
</script>
</asp:Content>