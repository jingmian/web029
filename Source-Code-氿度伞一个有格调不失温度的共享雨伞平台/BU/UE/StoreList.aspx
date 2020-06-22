<%@ Page Language="C#" AutoEventWireup="true" CodeFile="StoreList.aspx.cs" Inherits="ZoomLaCMS.BU.UE.StoreList" MasterPageFile="~/BU/UE/Master/MobileUser.master" EnableViewState="false"%>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>店铺列表</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
<asp:HiddenField runat="server" ID="Data_Hid" Value="" />
<div ng-app="app" ng-controller="appCtrl">
    <div ng-repeat="item in list track by $index">
   
<div class="map_text">
<div class="container">
<div class="map_text_list">
<div class="media">
<div class="media-left media-middle">
<a href="/Store/StoreIndex?id={{item.GeneralID}}">
<img  ng-src="{{'/UploadFiles/'+item.logo}}" alt="{{item.Title}}" onerror="javascript:this.src='/UploadFiles/nopic.gif'" />
</a>
</div>
<div class="media-body">
<h4 class="media-heading" ng-bind="item.Title"></h4>
<p><a href="javascript:;" onClick="ShowMap({{item.map}},'{{item.Title}}','{{item.addr}}',{{item.GeneralID}});">店铺地址:{{item.addr}}</a></p>
<p><i class="fa fa-map-marker" aria-hidden="true"></i> <span id="mi{{item.GeneralID}}"></span> {{item.distance}}公里</p>
<a href="/Store/StoreIndex?id={{item.GeneralID}}">马上借伞</a>
<a href="javascript:;"  onClick="qrcode();">马上还伞</a>
</div>
</div>
</div>
</div>
</div>
</div>
    <tm-pagination conf="pageConf"></tm-pagination>
</div>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Script">
<script src="/JS/Plugs/angular.min.js"></script>
<script src="/Plat/Note/tm.pagination.js"></script>
<script type="text/javascript" src="http://api.map.baidu.com/api?v=2.0&ak=BGDnQmbqyuguGgk2H948rEfd5QIjlqHd"></script>
<script>
var map = new BMap.Map("allmap");
var geolocation = new BMap.Geolocation();
angular.module("app", ['tm.pagination']).controller("appCtrl", function ($scope) {
    //初始化分页
    $scope.list = [];
    var list = JSON.parse($("#Data_Hid").val());
    $scope.pageConf = {
        currentPage: 1,
        totalItems: list.length,
        itemsPerPage: 10,
        pagesLength: 10,
        perPageOptions: [10, 20, 30, 40, 50],
        rememberPerPage: 'perPageItems',
        onChange: function () {
            $scope.list = $scope.getList($scope.pageConf.currentPage,$scope.pageConf.itemsPerPage,list);
        }
    };
    //--------------
    $scope.getList = function (cpage, psize, array) {
        //var psize = $scope.pageConf.pagesLength;
        var offset = (cpage - 1) * psize;
        return (offset + psize >= array.length) ? array.slice(offset, array.length) : array.slice(offset, offset + psize);
    }
    geolocation.getCurrentPosition(function (r) {
        if (this.getStatus() == BMAP_STATUS_SUCCESS) {
            var pointA = new BMap.Point(r.point.lng, r.point.lat);  // 创建点坐标A
            for (var i = 0; i < list.length; i++) {
                list[i].lng=list[i].map.split(',')[0];
                list[i].lat=list[i].map.split(',')[1];
                var pointB = new BMap.Point(list[i].lng,list[i].lat);  // 创建点坐标B
                list[i].distance = (map.getDistance(pointA, pointB) / 1000).toFixed(2);
            }
            list.sort(function (a, b) {
                return a.distance - b.distance;
            });
            
            $scope.list = $scope.getList($scope.pageConf.currentPage, $scope.pageConf.itemsPerPage, list);
            $scope.$digest();
        }

    }, { enableHighAccuracy: true });

});

</script>
</asp:Content>
