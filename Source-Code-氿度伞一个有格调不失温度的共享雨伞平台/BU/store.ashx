<%@ WebHandler Language="C#" Class="store" %>
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using ZoomLa.BLL;
using ZoomLa.BLL.API;
using ZoomLa.BLL.CreateJS;
using ZoomLa.BLL.Helper;
using ZoomLa.Model;
using ZoomLa.Model.CreateJS;
using ZoomLa.SQLDAL;
using Newtonsoft.Json;
using ZoomLa.SQLDAL.SQL;
using ZoomLa.BLL.User.Addon;
using ZoomLa.Model.User;
using ZoomLa.Sns;
using ZoomLa.Model.Shop;
using ZoomLa.BLL.Shop;
public class store : API_Base, IHttpHandler
{
    B_User buser = new B_User();
    B_OrderList orderBll = new B_OrderList();
    B_Payment payBll = new B_Payment();
    M_UserInfo mu = new M_UserInfo();
    B_Store_Info storeBll = new B_Store_Info();
    public void ProcessRequest(HttpContext context)
    {
        retMod.retcode = M_APIResult.Failed;
        retMod.callback = "";//禁止jsonp
        HttpRequest req = context.Request;
        string err = "";
        if (mu.IsNull) { retMod.retmsg = "用户未登录"; RepToClient(retMod); return; }
        if (!B_User.CheckUserStatus(mu, ref err)) { retMod.retmsg = err; RepToClient(retMod); return; }
        if (mu.GroupID != 2) { retMod.retmsg = "无权限访问该接口";RepToClient(retMod);return; }
        //-----------------------------
        try
        {
            switch (Action.ToLower())//改为不匹分大小写
            {
                case "ue_return"://商家确认还伞
                    {
                        string orderNo = Req("OrderNo");
                        M_OrderList orderMod = orderBll.SelModelByOrderNo(orderNo);
                        if (orderMod == null) { retMod.retmsg = "订单不存在"; }
                        else if (orderMod.OrderStatus != (int)M_OrderList.StatusEnum.DrawBack) { retMod.retmsg = "该订单未审请归还"; }
                        else
                        {
                            orderMod.Ordermessage = "用户归还";
                            Unbrealla.FinalOrder(orderMod);
                            retMod.retcode = M_APIResult.Success;
                        }
                    }
                    break;

                default:
                    retMod.retcode = M_APIResult.Failed;
                    retMod.retmsg = Action + "不是有效的参数";
                    break;
            }
        }
        catch (Exception ex) { retMod.retcode = M_APIResult.Failed; retMod.retmsg = ex.Message; }
        RepToClient(retMod);
    }

    public bool IsReusable { get { return false; } }
}