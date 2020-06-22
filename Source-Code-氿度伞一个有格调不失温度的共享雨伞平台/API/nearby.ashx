<%@ WebHandler Language="C#" Class="nearby" %>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using ZoomLa.BLL.API;
using ZoomLa.SQLDAL;
using ZoomLa.BLL;
using ZoomLa.BLL.Helper;
using Newtonsoft.Json;

public class nearby : System.Web.SessionState.IReadOnlySessionState, IHttpHandler
{
    public M_APIResult retMod = new M_APIResult();
    public string Action { get { return Req("Action").ToLower(); } }
    public string CallBack { get { return Req("callback"); } }
    public string Req(string name) { return HttpContext.Current.Request[name] ?? ""; }
    private static Dictionary<string, DataTable> dic = new Dictionary<string, DataTable>();
    //必须为地址栏
    private int CPage { get {  int _page= DataConvert.CLng(Req("page"));if (_page < 1) { _page = 1; }return _page; } }
    private int PSize { get { int _psize= DataConvert.CLng(Req("psize"));if (_psize < 1) { _psize = 10; }return _psize; } }
    public void RepToClient(M_APIResult result)
    {
        HttpResponse rep = HttpContext.Current.Response;
        rep.Clear(); rep.Write(result.ToString()); rep.Flush(); rep.End();
    }
    //仅用于313,周边景点
    public void ProcessRequest(HttpContext context)
    {
        //function.WriteErrMsg("接口默认关闭,请联系管理员开启");
        //M_UserInfo mu = buser.GetLogin();
        retMod.retcode = M_APIResult.Failed;
        //retMod.callback = CallBack;//暂不开放JsonP
        try
        {
            switch (Action)
            {
                case "mynear"://取出我周边指定范围内的景点
                    {
                        //string theme = Req("theme");
                        int limit = DataConvert.CLng(Req("limit"));//多少米范围之内
                        double lng = DataConvert.CDouble(Req("lng"));
                        double lat = DataConvert.CDouble(Req("lat"));
                        //-------
                        string key = "|" + lng + "|" + lat;
                        //dic = new Dictionary<string, DataTable>();
                        //retMod.addon = "FromSQL";
                        List<SqlParameter> sp = new List<SqlParameter>();
                        string where = "A.ModelID=24 And A.Status=99 and (NodeID=19 or FirstNodeID=19)";
                        //if (!string.IsNullOrEmpty(theme)) { where += " AND B.ThemeType LIKE @theme"; sp.Add(new SqlParameter("theme", "%" + theme + "%")); }
                        //DataTable nearDT = SqlHelper.JoinQuery(fields, "ZL_CommonModel", "ZL_C_JDMX", "A.ItemID=B.ID", where, "", sp.ToArray());
                        //DataTable nearDT = DBCenter.JoinQuery(fields, "ZL_CommonModel", "ZL_C_JDMX", "A.ItemID=B.ID", where, "", sp.ToArray());

                        DataTable nearDT = SqlHelper.ExecuteTable("SELECT GeneralID AS gid,Title AS title,CreateTime AS createtime,NodeID AS nodeid,B.addr,B.content,B.logo,B.lxr,B.synopsis,B.map,distnum=0,distance='' FROM ZL_CommonModel A LEFT JOIN ZL_Store_reg B ON A.ItemID=B.ID WHERE "+where,sp.ToArray());
                        for (int i = 0; i < nearDT.Rows.Count; i++)
                        {
                            DataRow dr = nearDT.Rows[i];
                            double rLng = 0, rLat = 0;
                            //string dtzb = DataConvert.CStr(dr["bdmap"]);
                            //if (string.IsNullOrEmpty(dtzb)) { nearDT.Rows.Remove(dr); continue; }
                            BaiduMap.GetPointByStr(dtzb, out rLng, out rLat);
                            dr["distnum"] = BaiduMap.GetShortDistance(lng, lat, rLng, rLat);
                            dr["distance"] = BaiduMap.ConvertToRoute(Convert.ToDouble(dr["distnum"]));
                        }
                        //gid,Title AS title,B.pic,B.dtzb,distnum=0,distance=''
                        if (limit > 0)
                        {
                            nearDT.DefaultView.RowFilter = "distnum<'" + limit + "'";
                            nearDT = nearDT.DefaultView.ToTable();
                        }
                        nearDT.DefaultView.Sort = "distnum ASC";
                        nearDT = nearDT.DefaultView.ToTable(true, "gid,title,establishment,pic,score,phone,address,businesstime,major,delivery,distnum,distance".Split(','));
                        //dic.Add(key, nearDT);
                        //if (!dic.ContainsKey(key))
                        //{

                        //}
                        //else { retMod.addon = "FromCache"; }
                        //DataTable cacheDT = dic[key];
                        DataTable resultDT = GetPagedTable(nearDT, CPage, PSize);
                        retMod.page = new M_API_Page() { cpage = CPage, itemCount = nearDT.Rows.Count, pageCount = PageCommon.GetPageCount(PSize,nearDT.Rows.Count) };
                        retMod.result = JsonConvert.SerializeObject(resultDT);
                        retMod.retcode = M_APIResult.Success;
                    }
                    break;
                case "nearpro":
                    {
                        int proid = DataConvert.CLng(Req("proid"));
                        double lng = DataConvert.CDouble(Req("lng"));
                        double lat = DataConvert.CDouble(Req("lat"));
                        DataTable ShopDT = SqlHelper.ExecuteTable(CommandType.Text, "select GeneralID,bdmap,distnum=0,distance='' from ZL_CommonModel left join ZL_Store_reg on ItemID=ID where Status=99 And ModelID=24", null);
                        for (int i = 0; i < ShopDT.Rows.Count; i++)
                        {
                            DataRow dr = ShopDT.Rows[i];
                            double rLng = 0, rLat = 0;
                            string dtzb = DataConvert.CStr(dr["bdmap"]);
                            if (string.IsNullOrEmpty(dtzb)) { ShopDT.Rows.Remove(dr); continue; }
                            BaiduMap.GetPointByStr(dtzb, out rLng, out rLat);
                            dr["distnum"] = BaiduMap.GetShortDistance(lng, lat, rLng, rLat);
                            dr["distance"] = BaiduMap.ConvertToRoute(Convert.ToDouble(dr["distnum"]));
                        }
                        DataTable ProDT = SqlHelper.ExecuteTable(CommandType.Text, "select ZL_Commodities.id,addtime,proname,thumbnails,shiprice,linprice,ys,usershopid,storename=(select StoreName from ZL_CommonModel left join ZL_Store_reg on ItemID=ID where GeneralID=ZL_Commodities.UserShopID),distnum=0,distance='' from ZL_Commodities left join ZL_S_shop on ItemID=ZL_S_shop.ID where UserShopID>0 And Istrue=1 And Sales=1 And Recycler=0 And Proname=(select Proname from ZL_Commodities where ID=" + proid + ") And ZL_Commodities.ID<> " + proid + " And ProClass=" + Req("proclass"), null);
                        for (int i = 0; i < ProDT.Rows.Count; i++)
                        {
                            DataRow dr = ProDT.Rows[i];
                            DataTable dt = ShopDT;
                            dt.DefaultView.RowFilter = "GeneralID=" + dr["UserShopID"];
                            dt = dt.DefaultView.ToTable();
                            if(dt.Rows.Count>0)
                            {
                                dr["distnum"] = dt.Rows[0]["distnum"];
                                dr["distance"] = dt.Rows[0]["distance"];
                            }
                        }
                        ProDT.DefaultView.Sort = "distnum ASC,AddTime DESC";
                        ProDT = ProDT.DefaultView.ToTable();
                        DataTable resultDT = GetPagedTable(ProDT, CPage, PSize);
                        retMod.page = new M_API_Page() { cpage = CPage, itemCount = ProDT.Rows.Count, pageCount = PageCommon.GetPageCount(PSize,ProDT.Rows.Count) };
                        retMod.result = JsonConvert.SerializeObject(resultDT);
                        retMod.retcode = M_APIResult.Success;
                    }
                    break;
                default:
                    {
                        retMod.retmsg = "[" + Action + "]接口不存在";
                    }
                    break;
            }
        }
        catch (Exception ex) { retMod.retmsg = ex.Message; }
        RepToClient(retMod);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
    public static DataTable GetPagedTable(DataTable dt, int PageIndex, int PageSize)
    {
        if (PageIndex == 0) { return dt; }
        DataTable newdt = dt.Copy();
        newdt.Clear();
        int rowbegin = (PageIndex - 1) * PageSize;
        int rowend = PageIndex * PageSize;

        if (rowbegin >= dt.Rows.Count)
        { return newdt; }

        if (rowend > dt.Rows.Count)
        { rowend = dt.Rows.Count; }
        for (int i = rowbegin; i <= rowend - 1; i++)
        {
            DataRow newdr = newdt.NewRow();
            DataRow dr = dt.Rows[i];
            foreach (DataColumn column in dt.Columns)
            {
                newdr[column.ColumnName] = dr[column.ColumnName];
            }
            newdt.Rows.Add(newdr);
        }
        return newdt;
    }
    /// <summary>
    /// 提供地图的辅助类
    /// </summary>
    public class BaiduMap
    {
        static double DEF_PI = 3.14159265359; // PI
        static double DEF_2PI = 6.28318530712; // 2*PI
        static double DEF_PI180 = 0.01745329252; // PI/180.0
        static double DEF_R = 6370693.5; // radius of earth

        /// <summary>
        /// 适用于近距离
        /// </summary>
        /// <returns></returns>
        public static double GetShortDistance(double lng1, double lat1, double lng2, double lat2)
        {
            double ew1, ns1, ew2, ns2;
            double dx, dy, dew;
            double distance;
            // 角度转换为弧度
            ew1 = lng1 * DEF_PI180;
            ns1 = lat1 * DEF_PI180;
            ew2 = lng2 * DEF_PI180;
            ns2 = lat2 * DEF_PI180;
            // 经度差
            dew = ew1 - ew2;
            // 若跨东经和西经180 度，进行调整
            if (dew > DEF_PI)
                dew = DEF_2PI - dew;
            else if (dew < -DEF_PI)
                dew = DEF_2PI + dew;
            dx = DEF_R * Math.Cos(ns1) * dew; // 东西方向长度(在纬度圈上的投影长度)
            dy = DEF_R * (ns1 - ns2); // 南北方向长度(在经度圈上的投影长度)
                                      // 勾股定理求斜边长
            distance = Math.Sqrt(dx * dx + dy * dy);
            return distance;
        }
        //适用于远距离
        public static double GetLongDistance(double lng1, double lat1, double lng2, double lat2)
        {
            double ew1, ns1, ew2, ns2;
            double distance;
            // 角度转换为弧度
            ew1 = lng1 * DEF_PI180;
            ns1 = lat1 * DEF_PI180;
            ew2 = lng2 * DEF_PI180;
            ns2 = lat2 * DEF_PI180;
            // 求大圆劣弧与球心所夹的角(弧度)
            distance = Math.Sin(ns1) * Math.Sin(ns2) + Math.Cos(ns1) * Math.Cos(ns2) * Math.Cos(ew1 - ew2);
            // 调整到[-1..1]范围内，避免溢出
            if (distance > 1.0)
                distance = 1.0;
            else if (distance < -1.0)
                distance = -1.0;
            // 求大圆劣弧长度
            distance = DEF_R * Math.Acos(distance);
            return distance;
        }

        public static bool GetPointByStr(string json, out double lng, out double lat)
        {
            try
            {
                lng = -1; lat = -1;
                if (json.StartsWith("["))//完全地图
                {
                    List<M_Map_Marker> list = JsonConvert.DeserializeObject<List<M_Map_Marker>>(json);
                    if (list.Count < 1) { return false; }
                    lng = Convert.ToDouble(list[0].mark.lng);
                    lat = Convert.ToDouble(list[0].mark.lat);
                }
                else
                {
                    json = json.Replace("=", "");
                    lng = Convert.ToDouble(json.Split(',')[0]);
                    lat = Convert.ToDouble(json.Split(',')[1]);
                }
            }
            catch (Exception ex) { throw new Exception(json + ":" + ex.Message); }
            return true;
        }
        public static string ConvertToRoute(double distance)
        {
            string result = "";
            if (distance < 100)
            {
                result = Convert.ToInt32(distance).ToString() + "米";
            }
            else
            {
                distance = distance / 1000;
                result = distance.ToString("f2") + "公里";
            }
            return result;
        }
    }
    public class M_Map_Point
    {
        public string lng = "";
        public string lat = "";
    }

    //107.613827,29.013192(lng,lat)
    //[{"mark":{"lng":104.873626,"lat":26.587309},"content":"%3Cp%3E%E8%90%A8%E8%BE%BE%E8%90%A8%E8%BE%BE%E6%92%92%E6%97%A6%E6%92%92%3C/p%3E","icon":"/Common/Label/Map/Img/f1.png"}]
    public class M_Map_Marker
    {
        /// <summary>
        /// 实际存的是坐标
        /// </summary>
        public M_Map_Point mark { get; set; }
        public string content = "";
        public string icon = "";
        //两个景点之间的坐标
        public double distance = 0;
    }
}