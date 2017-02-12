using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using WpfWebdown.Com;
using xNet;

namespace WpfWebdown
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        StringBuilder strBuder = new StringBuilder();
        private void UrlDown_Click(object sender, RoutedEventArgs e)
        {
            //youdao
            //post:  http://fanyi.youdao.com/translate?smartresult=dict&smartresult=rule&smartresult=ugc&sessionFrom=null
            string uri = this.UriText.Text;
            uri = "http://fanyi.youdao.com/";
            #region youdao Api


            //////////////////////////////////////////////////////////----------------------api
            //   http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=<doctype>&version=1.1&q=要翻译的文本
            //            有道翻译api
            //每小时1000次
            //API key：1820385571
            //keyfrom：webapifanyi
            //创建时间：2017 - 02 - 06
            //网站名称：webapifanyi
            //网站地址：http://oliven.cn
            //            olivenart @qq.com

            /*
                                 版本：1.1，请求方式：get，编码方式：utf-8
                    主要功能：中英互译，同时获得有道翻译结果和有道词典结果（可能没有）
                    参数说明：
　                    type - 返回结果的类型，固定为data
　                    doctype - 返回结果的数据格式，xml或json或jsonp
　                    version - 版本，当前最新版本为1.1
　                    q - 要翻译的文本，必须是UTF-8编码，字符长度不能超过200个字符，需要进行urlencode编码
　                    only - 可选参数，dict表示只获取词典数据，translate表示只获取翻译数据，默认为都获取
　                    注： 词典结果只支持中英互译，翻译结果支持英日韩法俄西到中文的翻译以及中文到英语的翻译
                    errorCode：
　                    0 - 正常
　                    20 - 要翻译的文本过长
　                    30 - 无法进行有效的翻译
　                    40 - 不支持的语言类型
　                    50 - 无效的key
　                    60 - 无词典结果，仅在获取词典结果生效
                    xml数据格式举例
                    http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=xml&version=1.1&q=这里是有道翻译API
                    <?xml version="1.0" encoding="UTF-8"?>
                    <youdao-fanyi>
                        <errorCode>0</errorCode>
                        <!-- 有道翻译 -->
                        <query><![CDATA[这里是有道翻译API]]></query>
                        <translation>
                            <paragraph><![CDATA[Here is the youdao translation API]]></paragraph>
                        </translation>
                    </youdao-fanyi>
                    json数据格式举例
                    http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=json&version=1.1&q=good
                    {
                        "errorCode":0
                        "query":"good",
                        "translation":["好"], // 有道翻译
                        "basic":{ // 有道词典-基本词典
                            "phonetic":"gʊd"
                            "uk-phonetic":"gʊd" //英式发音
                            "us-phonetic":"ɡʊd" //美式发音
                            "explains":[
                                "好处",
                                "好的"
                                "好"
                            ]
                        },
                        "web":[ // 有道词典-网络释义
                            {
                                "key":"good",
                                "value":["良好","善","美好"]
                            },
                            {...}
                        ]
                    }
                    jsonp数据格式举例
                    http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=jsonp&callback=show&version=1.1&q=API
                    show({
                        "errorCode":0
                        "query":"API",
                        "translation":["API"], // 有道翻译
                        "basic":{ // 有道词典-基本词典
                            "explains":[
                                "abbr. 应用程序界面（Application Program Interface）；..."
                            ]
                        },
                        "web":[ // 有道词典-网络释义
                            {
                                "key":"API",
                                "value":["应用程序接口(Application Programming Interface)","应用编程接口","应用程序编程接口","美国石油协会"]
                            },
                            {...}
                        ]
                    });
                                 */
            #endregion


            uri = "http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=json&version=1.1&q=";
            //"http://fanyi.youdao.com/openapi.do?keyfrom=webapifanyi&key=1820385571&type=data&doctype=<doctype>&version=1.1&q=";
            uri = uri + "если указанный HTTP-заголовок содержится, иначе значение ";// Convert.ToBase64String( Encoding.UTF8.GetBytes("если указанный HTTP-заголовок содержится, иначе значение "));

            #region XNet    https://habrahabr.ru/post/146475/


            using (var request = new HttpRequest(uri))
            {
                request.UserAgent = Http.ChromeUserAgent();
                //request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");

                request
                    // Parameters URL-address.
                    .AddUrlParam("data1", "value1")
                    .AddUrlParam("data2", "value2")

                    // Parameters 'x-www-form-urlencoded'.
                    .AddParam("data1", "value1")
                    .AddParam("data2", "value2")
                    .AddParam("data2", "value2")

                    // Multipart data.
                    .AddField("data1", "value1")
                    .AddFile("game_code", @"C:\Windows\HelpPane.exe")

                    // HTTP-header.
                    .AddHeader("X-Apocalypse", "21.12.12");

                // These parameters are sent in this request.
                request.Post("/").None();

                // But in this request they will be gone.
                request.Post("/").None();
            }

            //get
            using (var request = new HttpRequest())
            {
                request.UserAgent = Http.ChromeUserAgent();

                // Отправляем запрос.
                HttpResponse response = request.Get("habrahabr.ru");
                // Принимаем тело сообщения в виде строки.
                string content = response.ToString();
            }

            using (var request = new HttpRequest())
            {
                var urlParams = new RequestParams();

                urlParams["param1"] = "val1";
                urlParams["param2"] = "val2";

                string content = request.Get("habrahabr.ru", urlParams).ToString();
            }

            using (var request = new HttpRequest("habrahabr.ru"))
            {
                request.Get("/").None();
                request.Get("/feed").None();
                request.Get("/feed/posts");
            }

            using (var request = new HttpRequest("habrahabr.ru"))
            {
                request.Cookies = new CookieDictionary()
                      {
                               {"hash", "yrttsumi"},
                                  {"super-hash", "df56ghd"}
                         };

                request[HttpHeader.DNT] = "1";
                request["X-Secret-Param"] = "UFO";

                request.AddHeader("X-Tmp-Secret-Param", "42");
                request.AddHeader(HttpHeader.Referer, "http://site.com");

                request.Get("/");
            }
            //代理
            var proxyClient = HttpProxyClient.Parse("127.0.0.1:8080");
            var tcpClient = proxyClient.CreateConnection("habrahabr.ru", 80);

            try
            {
                using (var request = new HttpRequest())
                {
                    request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");
                    request.Get("habrahabr.ru");
                }
            }
            catch (HttpException ex)
            {
                Console.WriteLine("Произошла ошибка при работе с HTTP-сервером: {0}", ex.Message);

                switch (ex.Status)
                {
                    case HttpExceptionStatus.Other:
                        Console.WriteLine("Неизвестная ошибка");
                        break;

                    case HttpExceptionStatus.ProtocolError:
                        Console.WriteLine("Код состояния: {0}", (int)ex.HttpStatusCode);
                        break;

                    case HttpExceptionStatus.ConnectFailure:
                        Console.WriteLine("Не удалось соединиться с HTTP-сервером.");
                        break;

                    case HttpExceptionStatus.SendFailure:
                        Console.WriteLine("Не удалось отправить запрос HTTP-серверу.");
                        break;

                    case HttpExceptionStatus.ReceiveFailure:
                        Console.WriteLine("Не удалось загрузить ответ от HTTP-сервера.");
                        break;
                }
            }





            #endregion



            HttpWebclient httpweb = new HttpWebclient();
            string result = httpweb.GetWeb(uri);
            strBuder.Append(result);
            //uri = "http://fanyi.youdao.com/translate?smartresult=dict&smartresult=rule&smartresult=ugc&sessionFrom=null";




            this.Content.AppendText(strBuder.ToString());
        }

    }
}

