namespace ZoomLaCMS.PayOnline
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using ZoomLa.BLL;
    using ZoomLa.BLL.API;
    using ZoomLa.BLL.AlipayBank;
    using ZoomLa.BLL.Ebatong;
    using ZoomLa.BLL.MobaoPay;
    using ZoomLa.BLL.User.Develop;
    using ZoomLa.BLL.Helper;
    using ZoomLa.BLL.WxPayAPI;
    using ZoomLa.BLL.Shop;
    using ZoomLa.Common;
    using ZoomLa.Components;
    using ZoomLa.Model;
    using ZoomLa.Model.User.Develop;
    using ZoomLa.SQLDAL;
    using ZoomLa.BLL.Alipay;
    using ZoomLa.BLL.Pay;
    using ZoomLa.BLL.User.Addon;
    using ZoomLa.Model.User;
    using ZoomLa.BLL.User;
    

    public partial class PayOnline : System.Web.UI.Page
    {

        private M_Payment pinfo = new M_Payment();//支付信息，同于支付日志类,不过只记录用现金，银联等付款
        private M_Order_PayLog paylogMod = new M_Order_PayLog();
        private M_PayPlat payPlat = new M_PayPlat();
        private B_PayPlat payPlatBll = new B_PayPlat();//支付平台
        private B_Payment paymentBll = new B_Payment();
        private B_OrderList orderBll = new B_OrderList();//订单业务类
        private B_CartPro cartBll = new B_CartPro();//数据库中购物车业务类
        private B_Order_PayLog paylogBll = new B_Order_PayLog();//支付日志类,用于记录用户付款信息
        private B_User buser = new B_User();
        private OrderCommon orderCom = new OrderCommon();
        private string urlReq1 = "";//网站路径,用于设置回调页面(Disuse)
        public string PayMethod { get { return Request.QueryString["method"]; } }
        public string PayNo { get { return Request.QueryString["PayNo"]; } }
        public int StoreID { get { return DataConvert.CLng(Request["StoreID"],1); } }
        /*
         * 仅支持支付单
         */
        protected void Page_Load(object sender, EventArgs e)
        {
            B_User.CheckIsLogged(Request.RawUrl);
            M_UserInfo mu = buser.GetLogin(false);
            if (string.IsNullOrEmpty(SiteConfig.SiteInfo.SiteUrl)) { function.WriteErrMsg("错误,管理员未定义网站地址,SiteUrl"); }
            string siteurl = (SiteConfig.SiteInfo.SiteUrl.TrimEnd('/') + "/PayOnline/");
            if (string.IsNullOrEmpty(PayNo)) { function.WriteErrMsg("请传入支付单号"); }
            pinfo = paymentBll.SelModelByPayNo(PayNo);
            if (pinfo == null || pinfo.PaymentID < 1) { function.WriteErrMsg("支付单不存在"); }
            M_PayPlat payPlat = payPlatBll.SelReturnModel(pinfo.PayPlatID);
            if (!IsPostBack)
            {
                #region 母版页中信息
                logged_div.Visible = true;
                #endregion
                if (pinfo.Status != (int)M_Payment.PayStatus.NoPay) { function.WriteErrMsg("支付单不能重复付款"); }
                Rurl_Href.NavigateUrl = "/User/Order/OrderList";//返回我的订单
                if (pinfo.PaymentNum.Contains("IDC"))
                {
                    Rurl_Href.NavigateUrl = "/Plugins/Domain/ViewHave.aspx";
                }
                string url = Request.CurrentExecutionFilePath;
                urlReq1 = Request.Url.AbsoluteUri.ToString().Substring(0, Request.Url.AbsoluteUri.ToString().LastIndexOf('/'));
                double vmoney = pinfo.MoneyReal;//支付金额 
                string v_amount = pinfo.MoneyReal.ToString("f2"); //实际支付金额
                if (string.IsNullOrEmpty(PayMethod))
                {
                    #region 现金支付
                    DataTable orderDB1 = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);//订单表,ZL_OrderInfo
                    int orderType = 0;
                    if (orderDB1.Rows.Count > 0)
                    {
                        orderType = DataConvert.CLng(orderDB1.Rows[0]["OrderType"]);
                    }
                    if (orderType == 8)//有需要检测库存的订单类型，放此
                    {
                        if (!cartBll.CheckStock(Convert.ToInt32(orderDB1.Rows[0]["OrderType"])))
                        {
                            function.WriteErrMsg("商品库存数量不足，请重新购买");
                        }
                    }
                    DataTable ordertable = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);
                    if (pinfo.PayPlatID == 0 && !string.IsNullOrEmpty(pinfo.PlatformInfo))//支付宝网银支付 
                    {
                        payPlat = payPlatBll.SelModelByClass(M_PayPlat.Plat.Alipay_Bank);
                        alipayBank(pinfo.PlatformInfo);
                    }
                    if (payPlat.PayClass == 99)//线下支付
                    {
                        function.WriteSuccessMsg("信息已记录,请等待商家联系完成线下付款", Rurl_Href.NavigateUrl);
                    }
                    if (payPlat == null || payPlat.PayPlatID < 1)
                    {
                        function.WriteErrMsg("没有找到对应的支付平台信息!");
                    }
                    //StringBuilder v_urlBuilder = new StringBuilder();    //构造返回URL
                    //StringBuilder strHiddenField = new StringBuilder();
                    //string applicationName = paymentBll.GetApplicationName();
                    //if (!applicationName.EndsWith("/"))
                    //{
                    //    applicationName = applicationName + "/";
                    //}
                    //v_urlBuilder.Append("http://");
                    //v_urlBuilder.Append(applicationName);
                    //string v_ShowResultUrl = v_urlBuilder.ToString() + "PayOnline/ShowReturn.aspx?";
                    if (payPlat.PayClass == 100)//货到付款
                    {
                        payinfo_div.Visible = false;
                        AfterPay_Tb.Visible = true;
                        Title = "下单成功！";
                    }
                    else
                    {
                        payinfo_div.Visible = true;
                        AfterPay_Tb.Visible = false;
                    }
                    switch ((M_PayPlat.Plat)payPlat.PayClass)//现仅开通 12:支付宝即时到账和支付宝网银服务,15支付宝网银服务(上方代码中处理),银币与余额服务
                    {
                        #region 各种支付方式
                        case M_PayPlat.Plat.UnionPay:
                            #region 银联在线
                            //gateway = "https://pay3.chinabank.com.cn/PayGate?encoding=UTF-8";
                            //必要的交易信息
                            string wv_amount = v_amount;       // 订单金额
                            string wv_moneytype = "CNY";    // 币种
                            string wv_md5info;      // 对拼凑串MD5私钥加密后的值
                            string wv_mid = payPlat.AccountID;		 // 商户号
                            //v_urlBuilder.Append("http://localhost:86/PayOnline/PayReceive.aspx?PayID=" +payid);
                            string wv_url = urlReq1 + "/PayReceive.aspx?PayNo=" + PayNo;		 // 返回页地址
                            string wv_oid = pinfo.PayNo; // 推荐订单号构成格式为 年月日-商户号-小时分钟秒
                            //两个备注

                            // wv_mid = "1001";				 商户号，这里为测试商户号20000400，替换为自己的商户号即可
                            // wv_url = "http://localhost/chinabank/Receive.aspx";  商户自定义返回接收支付结果的页面
                            // MD5密钥要跟订单提交页相同，如Send.asp里的 key = "test" ,修改""号内 test 为您的密钥
                            string wkey = payPlat.MD5Key;				 // 如果您还没有设置MD5密钥请登录我们为您提供商户后台，地址：https://merchant3.chinabank.com.cn/
                            // 登录后在上面的导航栏里可能找到“B2C”，在二级导航栏里有“MD5密钥设置”
                            // 建议您设置一个16位以上的密钥或更高，密钥最多64位，但设置16位已经足够了
                            wv_amount = v_amount;       // 订单金额
                            wv_moneytype = "CNY";    // 币种
                            //对拼凑串MD5私钥加密后的值
                            wv_mid = payPlat.AccountID;		 // 商户号
                            wv_oid = pinfo.PayNo;// 推荐订单号构成格式为 年月日-商户号-小时分钟秒

                            if (wv_oid == null || wv_oid.Equals(""))
                            {
                                DateTime dt = DateTime.Now;
                                string wv_ymd = dt.ToString("yyyyMMdd"); // yyyyMMdd
                                string wtimeStr = dt.ToString("HHmmss"); // HHmmss
                                wv_oid = wv_ymd + wv_mid + wtimeStr;
                            }
                            string text = wv_amount + wv_moneytype + wv_oid + wv_mid + wv_url + wkey; // 拼凑加密串
                            wv_md5info = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(text, "md5").ToUpper();
                            //strHiddenField.Append("<input type='hidden' name='v_md5info' value='" + wv_md5info + "'>\n");
                            //strHiddenField.Append("<input type='hidden' name='v_mid' value='" + wv_mid + "'>\n");
                            //strHiddenField.Append("<input type='hidden' name='v_oid' value='" + wv_oid + "'>\n");
                            //strHiddenField.Append("<input type='hidden' name='v_amount' value='" + wv_amount + "' >\n");
                            //strHiddenField.Append("<input type='hidden' name='v_moneytype' value='" + wv_moneytype + "'>\n");
                            //strHiddenField.Append("<input type='hidden' name='v_url' value='" + wv_url + "'>\n");
                            //    //以下几项只是用来记录客户信息，可以不用，不影响支付 
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvname' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvaddr' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvtel' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvpost' value='" + "" + "' >\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvemail' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_rcvmobile' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_ordername' value='" + "" + "' >\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_orderaddr' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_ordertel' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_orderpost' value='" + "" + "' >\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_orderemail' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='v_ordermobile' value='" + "" + "'>\n");
                            //    strHiddenField.Append("<input type='hidden' name='Package' value='" + Request.QueryString["Package"] + "'>\n");
                            #endregion
                            break;
                        case M_PayPlat.Plat.ChinaUnionPay:
                            {
                                #region 中国银联
                                Dictionary<string, string> param = new Dictionary<string, string>();

                                //以下信息非特殊情况不需要改动
                                param["version"] = "5.0.0";//版本号
                                param["encoding"] = "UTF-8";//编码方式
                                param["txnType"] = "01";//交易类型
                                param["txnSubType"] = "01";//交易子类
                                param["bizType"] = "000201";//业务类型
                                param["signMethod"] = "01";//签名方法
                                param["channelType"] = "08";//渠道类型
                                param["accessType"] = "0";//接入类型
                                param["frontUrl"] = siteurl + "/Return/ChinaUnionPay.aspx";  //前台通知地址      
                                param["backUrl"] = siteurl + "/Return/ChinaUnionPayNotify.aspx";  //后台通知地址
                                param["currencyCode"] = "156";//交易币种

                                //TODO 以下信息需要填写
                                param["merId"] =payPlat.AccountID;//商户号，请改自己的测试商户号，此处默认取demo演示页面传递的参数
                                param["orderId"] = pinfo.PayNo;//商户订单号，8-32位数字字母，不能含“-”或“_”，此处默认取demo演示页面传递的参数，可以自行定制规则
                                param["txnTime"] = DateTime.Now.ToString("yyyyMMddHHmmss");//订单发送时间，格式为YYYYMMDDhhmmss，取北京时间，此处默认取demo演示页面传递的参数，参考取法： DateTime.Now.ToString("yyyyMMddHHmmss")
                                param["txnAmt"] = ((int)(pinfo.MoneyReal * 100)).ToString();//交易金额，单位分，此处默认取demo演示页面传递的参数
                                LblHiddenValue.InnerHtml = PayHelper.BuildForm("https://101.231.204.80:5000/gateway/api/frontTransReq.do", param);
                                Alipay_Btn.Visible = true;
                                #endregion
                            }
                            break;
                        case M_PayPlat.Plat.Alipay_Instant:
                            #region 支付宝[即时到帐]
                            string input_charset1 = "utf-8";
                            string notify_url1 = urlReq1 + "/Return/AlipayNotify.aspx";//付完款后服务器AJAX通知的页面 要用 http://格式的完整路径，不允许加?id=123这类自定义参数
                            string return_url1 = urlReq1 + "/Return/AlipayReturn.aspx";//付完款后跳转的页面 要用 http://格式的完整路径，不允许加?id=123这类自定义参数
                            string show_url1 = "";
                            string sign_type1 = "MD5";
                            ///////////////////////以下参数是需要通过下单时的订单数据传入进来获得////////////////////////////////
                            //必填参数
                            string price1 = pinfo.MoneyReal.ToString("f2");//订单总金额，显示在支付宝收银台里的“商品单价”里
                            string logistics_fee1 = "0.00";//物流费用，即运费。
                            string logistics_type1 = "POST";//物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
                            string logistics_payment1 = "SELLER_PAY";//物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）
                            string out_trade_no1 = pinfo.PayNo;            //请与贵网站订单系统中的唯一订单号匹配
                            string subject1 = pinfo.Remark;                //订单名称，显示在支付宝收银台里的“商品名称”里，显示在支付宝的交易管理的“商品名称”的列表里。
                            string body1 = pinfo.Remark;                   //订单描述、订单详细、订单备注，显示在支付宝收银台里的“商品描述”里         		
                            string quantity1 = "1";              		   //商品数量，建议默认为1，不改变值，把一次交易看成是一次下订单而非购买一件商品。
                            string receive_name1 = "";                     //收货人姓名，如：张三
                            string receive_address1 = "";		           //收货人地址，如：XX省XXX市XXX区XXX路XXX小区XXX栋XXX单元XXX号
                            string receive_zip1 = "";             		   //收货人邮编，如：123456
                            string receive_phone1 = "";	                   //收货人电话号码，如：0571-81234567
                            string receive_mobile1 = "";           	       //收货人手机号码，如：13312341234
                            //---------------------
                            string receive_name = orderDB1.Rows[0]["Reuser"] + "";                 //收货人姓名，如：张三
                            string receive_address = orderDB1.Rows[0]["Jiedao"] + "";		                //收货人地址，如：XX省XXX市XXX区XXX路XXX小区XXX栋XXX单元XXX号
                            string receive_zip = orderDB1.Rows[0]["ZipCode"] + "";              			    //收货人邮编，如：123456
                            string receive_phone = orderDB1.Rows[0]["Phone"] + "";		    //收货人电话号码，如：0571-81234567
                            string receive_mobile = orderDB1.Rows[0]["MobileNum"] + "";            		    //收货人手机号码，如：13312341234
                            //扩展参数——第二组物流方式
                            //物流方式是三个为一组成组出现。若要使用，三个参数都需要填上数据；若不使用，三个参数都需要为空
                            //有了第一组物流方式，才能有第二组物流方式，且不能与第一个物流方式中的物流类型相同，
                            //即logistics_type="EXPRESS"，那么logistics_type_1就必须在剩下的两个值（POST、EMS）中选择
                            string logistics_fee_3 = "";                					//物流费用，即运费。
                            string logistics_type_3 = "";               					//物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
                            string logistics_payment_3 = "";           					    //物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）

                            //扩展参数——第三组物流方式
                            //物流方式是三个为一组成组出现。若要使用，三个参数都需要填上数据；若不使用，三个参数都需要为空
                            //有了第一组物流方式和第二组物流方式，才能有第三组物流方式，且不能与第一组物流方式和第二组物流方式中的物流类型相同，
                            //即logistics_type="EXPRESS"、logistics_type_1="EMS"，那么logistics_type_2就只能选择"POST"
                            string logistics_fee_4 = "";                					//物流费用，即运费。
                            string logistics_type_4 = "";               					//物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
                            string logistics_payment_4 = "";            					//物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）
                            //扩展功能参数——其他
                            string buyer_email1 = "";                    					//默认买家支付宝账号
                            string discount1 = "";                       					//折扣，是具体的金额，而不是百分比。若要使用打折，请使用负数，并保证小数点最多两位数
                            /////////////////////////////////////////////////////////////////////////////////////////////////////
                            //构造请求函数，无需修改
                            B_AliPay_trades_Services aliService1 = new B_AliPay_trades_Services(
                            payPlat.AccountID,
                            payPlat.SellerEmail,
                            return_url1,
                            notify_url1,
                            show_url1,
                            out_trade_no1,
                            subject1,
                            body1,
                            price1,
                            logistics_fee1,
                            logistics_type1,
                            logistics_payment1,
                            quantity1,
                            receive_name1,
                            receive_address1,
                            receive_zip1,
                            receive_phone1,
                            receive_mobile1,
                            logistics_fee_3,
                            logistics_type_3,
                            logistics_payment_3,
                            logistics_fee_4,
                            logistics_type_4,
                            logistics_payment_4,
                            buyer_email1,
                            discount1,
                            payPlat.MD5Key,
                            input_charset1,
                            sign_type1);
                            Alipay_Btn.Visible = true;
                            LblHiddenValue.InnerHtml = aliService1.Build_Form();
                            #endregion
                            break;
                        case M_PayPlat.Plat.Alipay_H5:
                            {
                                Response.Redirect("/API/Pay/Alipay_H5.aspx?Payno=" + PayNo);
                            }
                            break;
                        case M_PayPlat.Plat.Alipay_Bank://支付宝网银,已上方处理
                            break;
                        case M_PayPlat.Plat.Bill:
                            #region 快钱支付
                            {
                                //人民币网关账户号
                                ///请登录快钱系统获取用户编号，用户编号后加01即为人民币网关账户号。
                                string merchantAcctId = payPlat.AccountID;
                                //人民币网关密钥
                                ///区分大小写.请与快钱联系索取
                                String key = payPlat.MD5Key;
                                //字符集.固定选择值。可为空。
                                ///只能选择1、2、3.
                                ///1代表UTF-8; 2代表GBK; 3代表gb2312
                                ///默认值为1
                                string inputCharset = "1";
                                //接受支付结果的页面地址.与[bgUrl]不能同时为空。必须是绝对地址。
                                ///如果[bgUrl]为空，快钱将支付结果Post到[pageUrl]对应的地址。
                                ///如果[bgUrl]不为空，并且[bgUrl]页面指定的<redirecturl>地址不为空，则转向到<redirecturl>对应的地址
                                ///
                                ///快钱通过服务器连接的方式将交易结果发送到[bgUrl]对应的页面地址，在商户处理完成后输出的<result>如果为1，页面会转向到<redirecturl>对应的地址。
                                ///如果快钱未接收到<redirecturl>对应的地址，快钱将把支付结果post到[pageUrl]对应的页面。
                                string bgUrl = "PayOnline/PayResultAlipayInstant.aspx?";
                                //网关版本.固定值
                                ///快钱会根据版本号来调用对应的接口处理程序。
                                ///本代码版本号固定为v2.0
                                string version = "v2.0";
                                //语言种类.固定选择值。
                                ///只能选择1、2、3
                                ///1代表中文；2代表英文
                                ///默认值为1
                                string language = "1";
                                //签名类型.固定值
                                ///1代表MD5签名
                                ///当前版本固定为1
                                string signType = "1";
                                //支付人姓名
                                ///可为中文或英文字符
                                string payerName = "payerName";
                                //支付人联系方式类型.固定选择值
                                ///只能选择1
                                ///1代表Email
                                string payerContactType = "1";
                                //支付人联系方式
                                ///只能选择Email或手机号
                                string payerContact = "";
                                //商户订单号
                                ///由字母、数字、或[-][_]组成
                                string orderId = pinfo.PayNo;// DateTime.Now.ToString("yyyyMMddHHmmss");
                                                            //订单金额
                                                            ///以分为单位，必须是整型数字
                                                            ///比方2，代表0.02元
                                double moneys = DataConverter.CDouble(v_amount);
                                moneys = moneys * 100;
                                string orderAmount = DataConverter.CLng(moneys).ToString();

                                //订单提交时间
                                ///14位数字。年[4位]月[2位]日[2位]时[2位]分[2位]秒[2位]
                                ///如；20080101010101
                                string orderTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                                //商品名称
                                ///可为中文或英文字符
                                string productName = "product";
                                //商品数量
                                ///可为空，非空时必须为数字
                                string productNum = "1";
                                //商品代码
                                ///可为字符或者数字
                                string productId = "1";
                                //商品描述
                                string productDesc = pinfo.Remark;
                                //扩展字段1
                                ///在支付结束后原样返回给商户
                                string ext1 = "";
                                //扩展字段2
                                ///在支付结束后原样返回给商户
                                string ext2 = "";
                                //支付方式.固定选择值
                                ///只能选择00、10、11、12、13、14
                                ///00：组合支付（网关支付页面显示快钱支持的各种支付方式，推荐使用）10：银行卡支付（网关支付页面只显示银行卡支付）.11：电话银行支付（网关支付页面只显示电话支付）.12：快钱账户支付（网关支付页面只显示快钱账户支付）.13：线下支付（网关支付页面只显示线下支付方式）.14：B2B支付（网关支付页面只显示B2B支付，但需要向快钱申请开通才能使用）
                                string payType = "00";

                                //银行代码
                                ///实现直接跳转到银行页面去支付,只在payType=10时才需设置参数
                                ///具体代码参见 接口文档银行代码列表
                                string bankId = "";

                                //同一订单禁止重复提交标志
                                ///固定选择值： 1、0
                                ///1代表同一订单号只允许提交1次；0表示同一订单号在没有支付成功的前提下可重复提交多次。默认为0建议实物购物车结算类商户采用0；虚拟产品类商户采用1
                                string redoFlag = "0";

                                //快钱的合作伙伴的账户号
                                ///如未和快钱签订代理合作协议，不需要填写本参数
                                string pid = "";
                                //生成加密签名串
                                ///请务必按照如下顺序和规则组成加密串！
                                String signMsgVal = "";
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "inputCharset", inputCharset);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "pageUrl", siteurl + "Return/Bill99Notify.aspx");
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "bgUrl", bgUrl);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "version", version);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "language", language);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "signType", signType);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "merchantAcctId", merchantAcctId);//merchantAcctId
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "payerName", payerName);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "payerContactType", payerContactType);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "payerContact", payerContact);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "orderId", orderId);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "orderAmount", orderAmount);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "orderTime", orderTime);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "productName", productName);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "productNum", productNum);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "productId", productId);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "productDesc", productDesc);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "ext1", ext1);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "ext2", ext2);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "payType", payType);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "bankId", bankId);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "redoFlag", redoFlag);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "pid", pid);
                                signMsgVal = Bill99Helper.appendParam(signMsgVal, "key", key);
                                string signMsg = StringHelper.MD5(signMsgVal).ToUpper();

                                Dictionary<string, string> payform_dics = new Dictionary<string, string>();
                                payform_dics.Add("inputCharset", inputCharset);
                                payform_dics.Add("bgUrl", bgUrl);
                                payform_dics.Add("pageUrl", siteurl + "Return/Bill99Notify.aspx");//服务器接受支付结果的后台地址.与[pageUrl]不能同时为空。必须是绝对地址。
                                payform_dics.Add("version", version);
                                payform_dics.Add("language", language);
                                payform_dics.Add("signType", signType);
                                payform_dics.Add("signMsg", signMsg);
                                payform_dics.Add("merchantAcctId", merchantAcctId);
                                payform_dics.Add("payerName", payerName);
                                payform_dics.Add("payerContactType", payerContactType);
                                payform_dics.Add("orderId", orderId);
                                payform_dics.Add("orderAmount", orderAmount);
                                payform_dics.Add("orderTime", orderTime);
                                payform_dics.Add("productName", productName);
                                payform_dics.Add("productNum", productNum);
                                payform_dics.Add("productId", productId);
                                payform_dics.Add("productDesc", productDesc);
                                payform_dics.Add("ext1", ext1);
                                payform_dics.Add("ext2", ext2);
                                payform_dics.Add("payType", payType);
                                payform_dics.Add("bankId", bankId);
                                payform_dics.Add("redoFlag", redoFlag);
                                payform_dics.Add("pid", pid);
                                payform_dics.Add("Package", Request.QueryString["Package"]);
                                LblHiddenValue.InnerHtml = PayHelper.BuildForm("https://www.99bill.com/gateway/recvMerchantInfoAction.htm", payform_dics);
                                Alipay_Btn.Visible = true;
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.WXPay:
                            #region 微信扫码支付
                            {
                                if (DeviceHelper.GetBrower() == DeviceHelper.Brower.Micro)
                                {
                                    if(StoreID<=1){
                                        Response.Redirect("/PayOnline/wxpay_mp.aspx?payno=" + pinfo.PayNo);
                                    }else{
                                        Response.Redirect("/PayOnline/wxpay_mp.aspx?payno=" + pinfo.PayNo+"&SuccessUrl=/Store/StoreIndex?id="+StoreID);
                                    }
                                    return;
                                }
                                pinfo.PlatformInfo = "0";
                                paymentBll.Update(pinfo);
                                WxPayData wxdata = new WxPayData();
                                wxdata.SetValue("out_trade_no", pinfo.PayNo);
                                wxdata.SetValue("body", string.IsNullOrEmpty(pinfo.Remark) ? "微信支付" : pinfo.Remark);
                                wxdata.SetValue("total_fee", Convert.ToInt32(pinfo.MoneyReal * 100));
                                wxdata.SetValue("trade_type", "NATIVE");
                                wxdata.SetValue("notify_url", urlReq1 + "/Return/WxPayReturn.aspx");
                                wxdata.SetValue("product_id", "1");
                                WxPayData result = WxPayApi.UnifiedOrder(wxdata, WxPayApi.Pay_GetByID());
                                if (result.GetValue("return_code").ToString().Equals("FAIL"))
                                    function.WriteErrMsg("商户" + result.GetValue("return_msg"));
                                Response.Redirect("/PayOnline/WxCodePay.aspx?PayNo=" + pinfo.PayNo + "&wxcode=" + result.GetValue("code_url"));
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.Ebatong:
                            #region Ebatong
                            {
                                Dictionary<string, string> ebatong_dics = new Dictionary<string, string>();
                                ebatong_dics.Add("sign_type", "MD5");
                                ebatong_dics.Add("service", "create_direct_pay_by_user");
                                ebatong_dics.Add("partner", payPlat.AccountID);
                                ebatong_dics.Add("input_charset", "UTF-8");
                                ebatong_dics.Add("notify_url", siteurl + "Return/EbatongNotify.aspx");//服务器异步通知页面路径
                                ebatong_dics.Add("return_url", siteurl + "Return/EbatongReturn.aspx");//服务器跳转页面
                                ebatong_dics.Add("out_trade_no", pinfo.PayNo);
                                ebatong_dics.Add("subject", pinfo.Remark);
                                ebatong_dics.Add("exter_invoke_ip", Request.UserHostAddress);
                                ebatong_dics.Add("payment_type", "1");
                                ebatong_dics.Add("seller_id", payPlat.AccountID);
                                ebatong_dics.Add("total_fee", pinfo.MoneyReal.ToString("f2"));
                                ebatong_dics.Add("error_notify_url", "");
                                ebatong_dics.Add("anti_phishing_key", new ZoomLa.BLL.Ebatong.AskForTimestamp().askFor(payPlat.AccountID, payPlat.MD5Key));
                                ebatong_dics.Add("seller_email", "");
                                ebatong_dics.Add("buyer_email", "");
                                ebatong_dics.Add("buyer_id", "");
                                ebatong_dics.Add("price", "");
                                ebatong_dics.Add("quantity", "");
                                ebatong_dics.Add("body", "");
                                ebatong_dics.Add("show_url", "");
                                ebatong_dics.Add("pay_method", "bankPay");
                                ebatong_dics.Add("extra_common_para", "");
                                ebatong_dics.Add("extend_param", "");
                                ebatong_dics.Add("it_b_pay", "");
                                ebatong_dics.Add("royalty_type", "");
                                ebatong_dics.Add("royalty_parameters", "");
                                ebatong_dics.Add("default_bank", "");
                                string[] paramts = new string[ebatong_dics.Keys.Count];//参数排序数组
                                ebatong_dics.Keys.CopyTo(paramts, 0);
                                Array.Sort(paramts);//参数排序操作
                                string paramstr = "";
                                foreach (string item in paramts)
                                {
                                    paramstr += string.Format("{0}={1}&", item, ebatong_dics[item]);
                                }
                                //throw new Exception(paramstr.Trim('&'));
                                string md5md5 = new ZoomLa.BLL.Ebatong.CommonHelper().md5("UTF-8", paramstr.Trim('&') + payPlat.MD5Key).ToLower();
                                ebatong_dics.Add("sign", md5md5);
                                LblHiddenValue.InnerHtml = PayHelper.BuildForm("https://www.ebatong.com/direct/gateway.htm", ebatong_dics);
                                Alipay_Btn.Visible = true;
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.CCB:
                            #region 江西建行
                            {
                                Dictionary<string, string> ccb_dics = new Dictionary<string, string>();
                                ccb_dics.Add("MERCHANTID", payPlat.AccountID);//商户代码
                                ccb_dics.Add("POSID", payPlat.PrivateKey);//柜台代码
                                ccb_dics.Add("BRANCHID", payPlat.PublicKey);//分行代码
                                ccb_dics.Add("ORDERID", pinfo.PayNo);
                                ccb_dics.Add("PAYMENT", pinfo.MoneyReal.ToString("f2"));
                                ccb_dics.Add("CURCODE", "01");
                                ccb_dics.Add("REMARK1", "");//备注信息1(具体信息待测试)
                                ccb_dics.Add("REMARK2", "");//备注信息2
                                ccb_dics.Add("TXCODE", "520100");
                                string ccb_paramstr = "";
                                foreach (String item in ccb_dics.Keys)
                                {
                                    ccb_paramstr += string.Format("{0}={1}&", item, ccb_dics[item]);
                                }
                                string md5str = new ZoomLa.BLL.Ebatong.CommonHelper().md5("UTF-8", ccb_paramstr.Trim('&')).ToLower();
                                ccb_dics.Add("MAC", md5str);
                                LblHiddenValue.InnerHtml = PayHelper.BuildForm("https://ibsbjstar.ccb.com.cn/app/ccbMain", ccb_dics);
                                Alipay_Btn.Visible = true;
                                //Response.Redirect("https://ibsbjstar.ccb.com.cn/app/ccbMain?" + ccb_paramstr + "MAC=" + md5str);
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.ECPSS:
                            #region 汇潮支付
                            {
                                Dictionary<string, string> payData = new Dictionary<string, string>();
                                payData.Add("OrderDesc", "test");//订单描述
                                payData.Add("Remark", "汇潮支付");//备注
                                payData.Add("AdviceURL", siteurl + "Return/ECPSSNotfy.aspx");//回调通知地址
                                payData.Add("ReturnURL", siteurl + "Return/ECPSSResult.aspx");//返回地址
                                payData.Add("BillNo", pinfo.PayNo);//订单号
                                payData.Add("MerNo", payPlat.AccountID);//商户号
                                payData.Add("Amount", pinfo.MoneyReal.ToString("f2"));//交易价格
                                string md5key = payPlat.MD5Key;
                                string md5str = payData["MerNo"] + "&" + payData["BillNo"] + "&" + payData["Amount"] + "&" + payData["ReturnURL"] + "&" + md5key;
                                payData.Add("SignInfo", System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(md5str, "MD5"));//签名
                                payData.Add("defaultBankNumber", "");//银行代码(选填)
                                payData.Add("orderTime", DateTime.Now.ToString("yyyyMMddHHmmss"));//交易时间yyyyMMddHHmmss
                                payData.Add("products", pinfo.Remark);//物品信息
                                LblHiddenValue.InnerHtml = PayHelper.BuildForm("https://pay.ecpss.com/sslpayment", payData);
                                Alipay_Btn.Visible = true;
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.ICBC_NC:
                            #region 南昌工行
                            {
                                function.WriteErrMsg("工行支付组件未注册,请联系管理员");
                                //ICBCHelper icbc = new ICBCHelper();
                                //infosecapiLib.infosecClass obj = new infosecapiLib.infosecClass();
                                //Dictionary<string, string> dics = new Dictionary<string, string>();
                                //string posturl = "https://B2C.icbc.com.cn/servlet/ICBCINBSEBusinessServlet";
                                //---Debug
                                //payPlat.AccountID = "1502EC24392836";
                                //payPlat.SellerEmail = "1502201009004747554";
                                //payPlat.PrivateKey = "/Cert/cs.key";
                                //payPlat.PublicKey = "/Cert/cs.cer";
                                //posturl = "https://myipad.dccnet.com.cn/servlet/NewB2cMerPayReqServlet";
                                //throw new Exception(payPlat.PrivateKey + ":" + payPlat.PublicKey + ":" + payPlat.AccountID + ":" + payPlat.SellerEmail);
                                //Debug End
                                //*.z01.com根据需要更改如*.hx008.com
                                //string data = icbc.SpliceTranData(pinfo, payPlat.AccountID.Trim(), payPlat.SellerEmail.Trim(), "*.z01.com", siteurl + "ICBCNotify.aspx");
                                //string sign = obj.sign(data, Server.MapPath(payPlat.PrivateKey), payPlat.MD5Key.Trim());//私钥虚拟路径与私钥密钥
                                //dics.Add("interfaceName", "ICBC_PERBANK_B2C");
                                //dics.Add("interfaceVersion", "1.0.0.11");
                                //dics.Add("tranData", obj.base64enc(data));
                                //dics.Add("merSignMsg", sign);
                                //dics.Add("merCert", icbc.ReadCertToBase64(payPlat.PublicKey));//公钥路径
                                //LblHiddenValue.InnerHtml = new Pay_BaoFa().BuildForm(posturl, dics);
                                //Alipay_Btn.Visible = true;
                            }
                            #endregion
                            break;
                        case M_PayPlat.Plat.CashOnDelivery:
                            #region 货到付款
                            zfpt.Text = payPlat.PayPlatName;
                            ddh.Text = pinfo.PaymentNum;
                            PayNum_L2.Text = Convert.ToDecimal(vmoney).ToString("F2") + " 元";
                            sxf.Text = payPlat.Rate.ToString() + " %";
                            sjhbje.Text = v_amount + " 元";
                            #endregion
                            break;
                        case M_PayPlat.Plat.PayPal:
                            {
                                Response.Redirect("PP/Pay.aspx?Payno="+pinfo.PayNo);
                            }
                            break;
                        default:
                            throw new Exception("错误:支付方式不存在：" + payPlat.PayClass);
                            #endregion
                    }
                    VMoneyPayed_L.Text = payPlat.PayPlatName;
                    OrderNo_L.Text = pinfo.PaymentNum;
                    LblRate.Text = payPlat.Rate.ToString() + " %";
                    #endregion
                }
                else//非现金支付,给用户显示确认支付页,实际支付行为在Confirm_Click
                {
                    spwd_div.Visible = (!string.IsNullOrEmpty(mu.PayPassWord));
                    nospwd_div.Visible = string.IsNullOrEmpty(mu.PayPassWord);//如未设定,则不需要输入二级密码
                    payinfo_div.Visible = true;
                    AfterPay_Tb.Visible = false;
                    OrderNo_L.Text = pinfo.PaymentNum;
                    DataTable ordertable = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);
                    if (ordertable != null && ordertable.Rows.Count > 0)
                    {
                        switch (PayMethod)
                        {
                            case "Purse":
                                //Titles.Text = "余额支付操作（确认支付款项)";
                                Fee.Text = "帐户余额：";
                                LblRate.Text = mu.Purse + " 元";
                                VMoneyPayed_L.Text = "帐户余额";
                                break;
                            case "SilverCoin":
                                //Titles.Text = "银币支付操作（确认支付款项)";
                                Fee.Text = "帐户银币：";
                                LblRate.Text = mu.SilverCoin + " 个";
                                VMoneyPayed_L.Text = "账户银币";
                                break;
                            case "Score":
                                //Titles.Text = "积分支付操作（确认支付款项)";
                                Fee.Text = "帐户积分：";
                                LblRate.Text = mu.UserExp + " 分";
                                VMoneyPayed_L.Text = "账户积分";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        payinfo_div.Visible = false;
                        AfterPay_Tb.Visible = false;
                        function.WriteErrMsg("订单不存在");
                    }
                }
                //显示金额信息
                LblPayMoney.Text = pinfo.MoneyReal.ToString("F2") + " 元";//应付金额
                //string priceJson = orderCom.GetTotalJson(ordertable);//多虚拟币支付,暂取消
                //M_LinPrice priceMod = JsonConvert.DeserializeObject<M_LinPrice>(priceJson);
                //if (orderCom.HasPrice(priceJson))
                //{
                //    if (priceMod.Purse > 0)
                //    {
                //        LblPayMoney.Text += " 余额:" + priceMod.Purse.ToString("f2");
                //    }
                //    if (priceMod.Sicon > 0)
                //    { LblPayMoney.Text += " 银币:" + priceMod.Sicon.ToString("f2"); }
                //    if (priceMod.Point > 0)
                //    {
                //        LblPayMoney.Text += " 积分:" + priceMod.Point.ToString("f2");
                //    }
                //}
            }
            //------------------检测End;
        }
        //虚拟币确认
        protected void Confirm_Click(object sender, EventArgs e)
        {
            M_UserInfo mu = buser.GetLogin(false);
            if (!string.IsNullOrEmpty(mu.PayPassWord))
            {
                if (!mu.PayPassWord.Equals(StringHelper.MD5(Request.Form["pwd_t"]))) { function.WriteErrMsg("支付密码不正确"); return; }
            }
            DataTable orderDT = new DataTable();
            orderDT = orderBll.GetOrderbyOrderNo(pinfo.PaymentNum);
            //-------------------
            #region 扣除附加币,暂注释
            //string priceJson = orderCom.GetTotalJson(orderDT);
            //if (orderCom.HasPrice(priceJson))//扣除附加
            //{
            //    M_LinPrice priceMod = JsonConvert.DeserializeObject<M_LinPrice>(priceJson);
            //    if (mu.Purse < priceMod.Purse)
            //    {
            //        function.WriteErrMsg("你的余额仅有" + mu.Purse + ",不够支付");
            //    }
            //    if (mu.SilverCoin < priceMod.Sicon)
            //    {
            //        function.WriteErrMsg("你的银币仅有" + mu.Purse + ",不够支付");
            //    }
            //    if (mu.UserExp < priceMod.Point)
            //    {
            //        function.WriteErrMsg("你的积分仅有" + mu.Purse + ",不够支付");
            //    }
            //    //将判断与扣减分开,避免出现扣减失误情况
            //    if (priceMod.Purse > 0)
            //    { buser.ChangeVirtualMoney(mu.UserID, new M_UserExpHis() { score = (int)-priceMod.Purse, ScoreType = 1, detail = pinfo.PaymentNum + "商城购物使用" + priceMod.Purse }); }
            //    if (priceMod.Sicon > 0)
            //    { buser.ChangeVirtualMoney(mu.UserID, new M_UserExpHis() { score = (int)-priceMod.Sicon, ScoreType = 2, detail = pinfo.PaymentNum + "商城购物使用" + priceMod.Sicon }); }
            //    if (priceMod.Point > 0)
            //    { buser.ChangeVirtualMoney(mu.UserID, new M_UserExpHis() { score = (int)-priceMod.Point, ScoreType = 3, detail = pinfo.PaymentNum + "商城购物使用" + priceMod.Point }); }
            //}
            #endregion
            //--------------------------------------------
            if (string.IsNullOrEmpty(PayMethod) && !string.IsNullOrEmpty(PayNo))//现金支付,跳转
            {

            }
            else if (!string.IsNullOrEmpty(PayMethod))//虚拟币支付
            {
                //paymentMod = paymentBll.SelModelByPayNo(PayNo);
                PayByVirtualMoney(PayMethod, pinfo);
            }
            else { function.WriteErrMsg("支付出错,支付单不存在"); }
        }
        //支付单虚拟币付款
        private void PayByVirtualMoney(string payMethod, M_Payment payMod)
        {
            M_UserInfo mui = buser.GetLogin(false);
            List<M_OrderList> orderList = OrderHelper.OrdersCheck(payMod);
            ActualAmount.Visible = false;
            paylogMod.UserID = mui.UserID;
            switch (payMethod)//完成支付
            {
                case "Purse":
                    if (!SiteConfig.SiteOption.SiteID.Contains("purse")) { function.WriteErrMsg("管理员已关闭余额支付功能!"); }
                    if (mui.Purse < (double)payMod.MoneyReal) { function.WriteErrMsg("对不起,余额不足! 请<a href='/PayOnline/OrderPay.aspx?Money=" + payMod.MoneyReal + "' target='_blank' style='margin-left:5px;color:#f00;'>充值!</a>"); }
                    buser.ChangeVirtualMoney(mui.UserID, new M_UserExpHis()
                    {
                        score = -(double)payMod.MoneyReal,
                        ScoreType = (int)M_UserExpHis.SType.Purse,
                        detail = "支付成功,支付单号:" + payMod.PayNo
                    });
                    mui = buser.GetLogin(false);
                    zfpt.Text = "余额";
                    fees.Text = "帐户余额：";
                    sxf.Text = mui.Purse + " 元";
                    break;
                case "SilverCoin":
                    if (!SiteConfig.SiteOption.SiteID.Contains("sicon")) { function.WriteErrMsg("管理员已关闭银币支付功能!"); }
                    if (mui.SilverCoin < (double)payMod.MoneyReal) { function.WriteErrMsg("对不起,银币不足!"); }
                    buser.ChangeVirtualMoney(mui.UserID, new M_UserExpHis()
                    {
                        score = -(double)payMod.MoneyReal,
                        ScoreType = (int)M_UserExpHis.SType.SIcon,
                        detail = "支付成功,支付单号:" + payMod.PayNo
                    });
                    mui = buser.GetLogin(false);
                    zfpt.Text = "银币";
                    fees.Text = "帐户银币：";
                    sxf.Text = mui.SilverCoin + " 个";
                    break;
                case "Score":
                    if (!SiteConfig.SiteOption.SiteID.Contains("point")) { function.WriteErrMsg("管理员已关闭积分支付功能!"); }
                    if (mui.UserExp < (double)payMod.MoneyReal) { function.WriteErrMsg("对不起,积分不足!"); }
                    buser.ChangeVirtualMoney(mui.UserID, new M_UserExpHis()
                    {
                        score = -(double)payMod.MoneyReal,
                        ScoreType = (int)M_UserExpHis.SType.Point,
                        detail = "支付成功,支付单号:" + payMod.PayNo
                    });
                    mui = buser.GetLogin(false);
                    zfpt.Text = "积分";
                    fees.Text = "帐户积分：";
                    sxf.Text = mui.UserExp + " 分";
                    break;
                default:
                    function.WriteErrMsg("指定的支付方式不存在,请检查大小写是否正确!");
                    break;
            }
            for (int i = 0; i < orderList.Count; i++)//更改订单状态
            {
                M_OrderList orderMod = orderList[i];
                OrderHelper.SaveSnapShot(orderMod);
                #region 写入日志,更新订单状态
                switch (payMethod)
                {
                    case "Purse":
                        orderMod.Paymentstatus = (int)M_OrderList.PayEnum.HasPayed;
                        orderMod.Receivablesamount = orderMod.Ordersamount;
                        if (orderBll.Update(orderMod))
                        {
                            orderCom.SendMessage(orderMod, paylogMod, "payed");
                            paylogMod.PayMethod = (int)M_Order_PayLog.PayMethodEnum.Purse;
                            paylogMod.Remind += "商城订单" + orderMod.OrderNo + "余额付款成功";
                        }
                        break;
                    case "SilverCoin":
                        orderMod.Paymentstatus = (int)M_OrderList.PayEnum.HasPayed;
                        orderMod.Receivablesamount = orderMod.Ordersamount;
                        if (orderBll.Update(orderMod))
                        {
                            orderCom.SendMessage(orderMod, paylogMod, "payed");
                            paylogMod.PayMethod = (int)M_Order_PayLog.PayMethodEnum.Silver;
                            paylogMod.Remind += "商城订单" + orderMod.OrderNo + "银币付款成功";
                        }
                        break;
                    case "Score":
                        orderMod.Paymentstatus = (int)M_OrderList.PayEnum.HasPayed;
                        orderMod.Receivablesamount = orderMod.Ordersamount;
                        if (orderBll.Update(orderMod))
                        {
                            orderCom.SendMessage(orderMod, paylogMod, "payed");
                            paylogMod.PayMethod = (int)M_Order_PayLog.PayMethodEnum.Score;
                            paylogMod.Remind = "商城订单" + orderMod.OrderNo + "积分付款成功";
                        }
                        break;
                    default:
                        function.WriteErrMsg("指定的支付方式不存在,请检查大小写是否正确!");
                        break;
                }
                //-----------------------付款后处理区域
                //orderCom.SaveSnapShot(orderMod);
                payMod.MoneyTrue = payMod.MoneyReal;
                OrderHelper.FinalStep(payMod,orderMod,paylogMod);//支付成功后处理步步骤,允许操作paylogMod
                #endregion
                
            }
            //-----------------For End
            ddh.Text = payMod.PaymentNum;
            PayNum_L2.Text = payMod.MoneyReal.ToString("f2");
            sjhbje.Text = payMod.MoneyReal.ToString("f2");
            payMod.Status = (int)M_Payment.PayStatus.HasPayed;
            payMod.CStatus = true;
            paymentBll.Update(payMod);
            payinfo_div.Visible = false;
            AfterPay_Tb.Visible = true;
        }
        //支付单现金跳转付款
        //private void PayByMoney(M_Payment payMod)
        //{
        //    switch ((M_PayPlat.Plat)payPlat.PayClass)
        //    {
        //        case M_PayPlat.Plat.ICBC_NC://南昌工行
        //            break;
        //        default:
        //            throw new Exception("付款方式不存在");
        //    }
        //}
        //支付宝单网银
        public void alipayBank(string bankName)
        {
            B_Alipay_Bank_Submit sub = new B_Alipay_Bank_Submit();//网银专用
            //-------------------支付宝网银支付
            ////////////////////////////////////////////请求参数////////////////////////////////////////////
            //支付类型
            string payment_type = "1";
            //必填，不能修改
            //服务器异步通知页面路径
            string notify_url = urlReq1 + "/AlipayReturn.aspx";
            //需http://格式的完整路径，不能加?id=123这类自定义参数
            //页面跳转同步通知页面路径
            string return_url = urlReq1 + "/AlipayNotify.aspx";
            //需http://格式的完整路径，不能加?id=123这类自定义参数，不能写成http://localhost/
            //卖家支付宝帐户
            string seller_email = payPlat.SellerEmail;
            //必填
            //商户订单号
            string out_trade_no = pinfo.PaymentNum;
            //商户网站订单系统中唯一订单号，必填
            //订单名称
            string subject = pinfo.Remark;
            //必填
            //付款金额
            string total_fee = pinfo.MoneyReal.ToString("f2");
            //订单描述
            string body = pinfo.Remark;
            //默认支付方式
            string paymethod = "bankPay";
            //必填
            //默认网银
            string defaultbank = bankName;
            //必填，银行简码请参考接口技术文档

            //商品展示地址
            string show_url = pinfo.PaymentNum;
            //需以http://开头的完整路径，例如：http://www.xxx.com/myorder.html

            //防钓鱼时间戳
            string anti_phishing_key = "";
            //若要使用请调用类文件submit中的query_timestamp函数
            //客户端的IP地址
            string exter_invoke_ip = "";
            //非局域网的外网IP地址，如：221.0.0.1
            ////////////////////////////////////////////////////////////////////////////////////////////////

            //把请求参数打包成数组
            SortedDictionary<string, string> sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("partner", payPlat.AccountID.Trim());
            sParaTemp.Add("_input_charset", "utf-8");
            sParaTemp.Add("service", "create_direct_pay_by_user");
            sParaTemp.Add("payment_type", payment_type);
            sParaTemp.Add("notify_url", notify_url);
            sParaTemp.Add("return_url", return_url);
            sParaTemp.Add("seller_email", seller_email);
            sParaTemp.Add("out_trade_no", out_trade_no);
            sParaTemp.Add("subject", subject);
            sParaTemp.Add("total_fee", total_fee);
            sParaTemp.Add("body", body);
            sParaTemp.Add("paymethod", paymethod);//支付方式,  string paymethod = "bankPay";
            sParaTemp.Add("defaultbank", defaultbank);//银行
            sParaTemp.Add("show_url", show_url);
            sParaTemp.Add("anti_phishing_key", anti_phishing_key);
            sParaTemp.Add("exter_invoke_ip", exter_invoke_ip);
            //建立请求
            string sHtmlText = sub.BuildRequest(sParaTemp, "get", "确认");
            Response.Write(sHtmlText);
        }
    }
}