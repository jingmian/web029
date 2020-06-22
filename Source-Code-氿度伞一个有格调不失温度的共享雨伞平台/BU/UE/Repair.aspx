<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Repair.aspx.cs" Inherits="ZoomLaCMS.BU.UE.Repair" MasterPageFile="~/Common/Master/Empty.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head">
    <title>我的雨伞</title>
    <link href="/dist/css/weui.min.css" rel="stylesheet" />
    <script src="/dist/js/weui.js"></script>
    <style>
        body { background: #f8f8f8; }
        .wei-footer { position: fixed; }
        .weui-btn-area { margin-top: 0.3em; }
        .order-item { font-family: "STHeiti","Microsoft YaHei","黑体","arial"; }
        .order-item { background: #fff; margin-top: 5px; border-bottom: 1px solid #e3e5e9; }
        .gray9 { color: #999; }
        .tips_div { padding-top: 10px; padding-bottom: 10px; padding-left: 15px; padding-right: 15px; border-top: 1px solid #e3e5e9; position: relative; }
        .prolist { padding: 5px 10px; }
        .prolist .media p { margin-bottom: 0; font-size: 0.9em; }
        .prolist .media p span { margin-right: 5px; }
        .weui-cell__bd p { margin-bottom:0;}
        label { margin-bottom:0; font-weight:500;}
    </style>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
    <div class="weui-cells__title">可操作伞列表</div>
<div class="weui-flex" style="border-top:1px solid #CCC; border-bottom:1px solid #CCC;"> 
<div class="weui-flex__item repair"><a href="/BU/UE/MyLend.aspx?StoreID=<%Call.Label("{$GetRequest(StoreID)$}");%>" class="weui-navbar__item" style="color:#333; border-right:1px solid #CCCCCC;">我的雨伞</a></div>
<div class="weui-flex__item"><a href="/BU/UE/Repair.aspx?StoreID=<%Call.Label("{$GetRequest(StoreID)$}");%>" class="weui-navbar__item weui-bar__item_on" style="color:#333;">报修提交</a></div>
</div>
    <asp:Repeater runat="server" ID="RPT" OnItemCommand="RPT_ItemCommand">
        <ItemTemplate>
            <div class="order-item">
                <div class="tips_div">
                    <div><span class="gray9">ID：<%#Eval("ID") %> | 伞号：<%#Eval("Balance_remark") %> | 状态：<%#ZoomLa.Sns.Unbrealla.T_GetStatus(Eval("OrderStatus")) %></span></div>
                    <div class="orderinfo"><span class="gray9">借出点：<%#Eval("StoreName") %></span></div>
                    <div class="orderinfo"><span class="gray9">借出时间：<%#Eval("AddTime","{0:yyyy年MM月dd日 HH:mm}") %></span></div>
                </div>
                
<div class="weui-cells weui-cells_radio" style="margin-top:0; <%#Eval("OrderStatus","")=="0"?"":"display:none;" %>">
<div class="weui-cell weui-check__label">
<label class="weui-cells_checkbox"><input type="checkbox" class="weui-check" name="idchk" value="<%#Eval("ID") %>"/><i class="weui-icon-checked"></i>是否报修</label>
</div>
<label class="weui-cell weui-check__label">
<div class="weui-cell__bd"><p>伞主杆损坏 5元</p></div>
<div class="weui-cell__ft"><input type="radio" class="weui-check"value="1"  name="<%#"repair"+Eval("ID")+"_rad" %>" checked="checked"/><span class="weui-icon-checked"></span></div>
</label>
<label class="weui-cell weui-check__label">
<div class="weui-cell__bd"><p>伞支架损坏 4元</p></div>
<div class="weui-cell__ft"><input type="radio" class="weui-check"value="1"  name="<%#"repair"+Eval("ID")+"_rad" %>" checked="checked"/><span class="weui-icon-checked"></span></div>
</label>
<label class="weui-cell weui-check__label">
<div class="weui-cell__bd"><p>伞主杆损坏 5元</p></div>
<div class="weui-cell__ft"><input type="radio" class="weui-check"value="1"  name="<%#"repair"+Eval("ID")+"_rad" %>" checked="checked"/><span class="weui-icon-checked"></span></div>
</label>

</div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <div runat="server" id="op_div" style="margin-top: 10px; background: #FFF; padding: 5px 10px; text-align: right;">
        <asp:Button runat="server" ID="BatReturn_Btn" OnClick="BatReturn_Btn_Click" class="btn" Text="批量还伞" OnClientClick="return calcCheck();" Style="background: #ee7218; color: #fff;" />
    </div>
    <%Call.Label("{ZL.Label id=\"微信底部\"/}");%>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
    <!--<script src="/JS/Mobile/vconsole.min.js"></script>-->
    <link href="/Plugins/Third/alert/sweetalert.css" rel="stylesheet" />
    <script src="/Plugins/Third/alert/sweetalert.min.js?v=1"></script>
<script>
if("<%Call.Label("{$GetRequest(StoreID)$}");%>"=="" || "<%Call.Label("{$GetRequest(StoreID)$}");%>"==null){
    $(".repair").hide();
}
</script>
</asp:Content>
