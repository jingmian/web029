<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MyLend.aspx.cs" Inherits="ZoomLaCMS.BU.UE.MyLend"  MasterPageFile="~/Common/Master/Empty.master"%>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>我的雨伞</title>
<link href="/dist/css/weui.min.css" rel="stylesheet" />
<script src="/dist/js/weui.js"></script>
<style>
body { background:#f8f8f8;}
.wei-footer { position:fixed;}
.weui-btn-area { margin-top:0.3em;}
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
<div class="weui-cells__title">可操作伞列表</div>
<div class="weui-flex" style="border-top:1px solid #CCC; border-bottom:1px solid #CCC;"> 
<div class="weui-flex__item"><a href="/BU/UE/MyLend.aspx?StoreID=<%Call.Label("{$GetRequest(StoreID)$}");%>" class="weui-navbar__item weui-bar__item_on" style="color:#333; border-right:1px solid #CCCCCC;">我的雨伞</a></div>
<div class="weui-flex__item repair"><a href="/BU/UE/Repair.aspx?StoreID=<%Call.Label("{$GetRequest(StoreID)$}");%>" class="weui-navbar__item" style="color:#333;">报修提交</a></div>
</div>
<asp:Repeater runat="server" ID="RPT" OnItemCommand="RPT_ItemCommand">
<ItemTemplate>
<div class="order-item">
<div class="tips_div">
<div><span class="gray9">ID：<%#Eval("ID") %> | 伞号：<%#Eval("Balance_remark") %> | 状态：<%#ZoomLa.Sns.Unbrealla.T_GetStatus(Eval("OrderStatus")) %></span></div>
<div class="orderinfo"><span class="gray9">借出点：<%#Eval("StoreName") %></span></div>
<div class="orderinfo"><span class="gray9">归还点：<%#Eval("RStoreName") %></span></div> 
<div class="orderinfo"><span class="gray9">借出时间：<%#Eval("AddTime","{0:yyyy年MM月dd日 HH:mm}") %></span></div>
</div>
<div class="prolist">
<p class="text-right" style="margin-bottom:0;<%#Eval("OrderStatus","")=="-1"?"display:none;":""%>" >
<a href="/BU/UE/Repair.aspx?oid=<%#Eval("ID")%>" class="btn hidden" style="background:#ee7218; color:#fff; float:left;"> 我要报修</a>
<a href="javascript:;" class="btn" style="background:#ee7218; color:#fff;" onClick="showLend(<%#Eval("ID")%>);"> 转借二维码</a>
<asp:LinkButton runat="server" CommandName="return" CommandArgument='<%#Eval("ID") %>' class="btn res" style="background:#ee7218; color:#fff;" Visible=''> 还伞提交</asp:LinkButton>
<a href="/Class_19/Default.aspx" class="btn res1" style="background:#ee7218; color:#fff;" > 还伞提交</a>
</p>
</div>
</div>
</ItemTemplate>
</asp:Repeater>
<div runat="server" id="op_div" style="margin-top:10px; background:#FFF; padding:5px 10px; text-align:right;">
<button type="button" class="btn" onclick="calc.minus();" style="background:#ee7218; color:#fff;"><i class="fa fa-minus"></i></button>
<input type="text" class="form-control" style="width:100px; text-align:center; display:inline-block;" id="returnNum" value="1" />
<button type="button" class="btn" onclick="calc.add();" style="background:#ee7218; color:#fff;"><i class="fa fa-plus"></i></button>
<asp:Button runat="server" ID="BatReturn_Btn" OnClick="BatReturn_Btn_Click" class="btn" Text="批量还伞" OnClientClick="return calcCheck();" style="background:#ee7218; color:#fff;"/>
<asp:HiddenField runat="server" ID="Num_Hid" />
</div>
<%Call.Label("{ZL.Label id=\"微信底部\"/}");%>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<!--<script src="/JS/Mobile/vconsole.min.js"></script>-->
<link href="/Plugins/Third/alert/sweetalert.css" rel="stylesheet" />
<script src="/Plugins/Third/alert/sweetalert.min.js?v=1"></script>
<script>
if("<%Call.Label("{$GetRequest(StoreID)$}");%>"=="" || "<%Call.Label("{$GetRequest(StoreID)$}");%>"==null){
    $(".res").hide();
    $(".repair").hide();
}else{
    $(".res1").hide();
}

$("#foot_user").addClass("weui-bar__item_on");
function showLend(oid) {
    var url = "<%Call.Label("{$SiteURL/}");%>/BU/UE/LentToUser.aspx?Oid="+oid;
    console.log(url);
    weui.dialog({
        title: '请扫码完成转借',
        content: '<img src="/Common/Common.ashx?url='+url+'" style="width:230px;" />',
        className: 'custom-classname',
        buttons: [{
            label: '关闭窗口',
            type: 'default',
            onClick: function () {  }
        }]
    });
}
</script>
<asp:HiddenField runat="server" ID="MaxCount_Hid" />
<script>
    var calc = {
        max: <%=MaxCount_Hid.Value%>, min: 0, $input: $("#returnNum"),
        get: function () {
            var ref = this;
            var r = parseInt(ref.$input.val());
            if ((r + "") == "NaN") { r = ref.min; }
            return r;
        },
        add: function () {
            var ref = this;
            var num = ref.get();
            num++;
            if (num >= ref.min && num <= ref.max) { ref.$input.val(num); }
        },
        minus: function () {
            var ref = this;
            var num = ref.get();
            num--;
            if (num >= ref.min && num <= ref.max) { ref.$input.val(num); }
        }
    };
    function calcCheck() {
        var num = calc.get();
        if (num < 1) { alert("数量必须大于0"); return false; }
        $("#Num_Hid").val(num);
        return true;
    }
</script>
</asp:Content>