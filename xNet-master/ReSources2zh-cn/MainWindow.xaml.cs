using mshtml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Xml;
using System.Net;

namespace ReSources2zh_cn
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Star();
        }
        private void Star()
        {
            this.webView.Source = new Uri("http://fanyi.youdao.com/");
        }        
        //Time
        Stopwatch stopwatch;
        //task 可以取消
        private CancellationTokenSource tokenSource;
        //同步上下文        
        SynchronizationContext m_SyncContext = null;
        private Task task1;
        private Task task2;
        private WebBrowser _web;
        //线程阻塞  http://www.cnblogs.com/tianzhiliang/archive/2011/03/04/1970726.html
        private ManualResetEvent _mre;      

   //open web and do someting
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            string url = this.url.Text;
            if (url.Contains("http"))
            {
                this.webView.Source = new Uri(url);
            }
            else
            {
                return;
            }
            //线程的取消
            tokenSource = new CancellationTokenSource();
            //跨线程的操作，获取当前Ui的上下文
            m_SyncContext = SynchronizationContext.Current;
            task1 = new Task(() =>
            {
                // you methed
                if (tokenSource.IsCancellationRequested)
                {
                    return;
                }
                m_SyncContext.Post(lable1, "task 执行中！" + ((int)(stopwatch.ElapsedMilliseconds)).ToString());
                for (int i = 0; i < 1000; i++)
                {
                    if (i % 100 == 0)
                    {
                        m_SyncContext.Post(lable1, i.ToString() + "--" + ((int)(stopwatch.ElapsedMilliseconds) / 1000).ToString());
                    }
                    if (tokenSource.IsCancellationRequested) { tokenSource.Token.ThrowIfCancellationRequested(); }

                    Console.WriteLine("--" + i);
                }
                m_SyncContext.Post(lable1, "task 执行中发出响应界面！" + ((int)(stopwatch.ElapsedMilliseconds)).ToString());

            }, tokenSource.Token);
            task1.Start();
            task2 = new Task(() =>
            {
                m_SyncContext.Post(lable1, "task2 执行" + ((int)(stopwatch.ElapsedMilliseconds)).ToString());

                ///////////////////////////////////////////////////---------- 调用线程必须为 STA, 因为许多 UI 组件都需要
                ///////Dispatcher线程管理器
                App.Current.Dispatcher.Invoke((() =>
                {
                    //_WebView();
                }));

            });
            //开始另一个task
            task1.ContinueWith(task1 => { task2.Start(); });

            //-------------------------------------------------------------------webbrowser 后台调用有问题
            _web = new WebBrowser();
            _web.Navigate("http://fanyi.youdao.com/");
            //_web.Source = new Uri("http://fanyi.youdao.com/");
            _web.LoadCompleted += Web_LoadCompleted;
            
            //带参数的线程
            //http://www.cnblogs.com/csMapx/archive/2011/06/20/2084647.html
            //Thread thrread = new Thread(_WebView);
            //thrread.Start(_web);//启动
            //thrread.Abort();//终止


            //http://www.cnblogs.com/xugang/archive/2010/04/20/1716042.html
            //向线程池中排入9个工作线程
          _mre=  new ManualResetEvent(false);//线程状态   http://www.cnblogs.com/xiaofengfeng/archive/2012/12/21/2828387.html
            for (int i = 1; i <= 9; i++)
            {
                //QueueUserWorkItem()方法：将工作任务排入线程池。
                ThreadPool.QueueUserWorkItem(new WaitCallback(Fun), i);
                // Fun 表示要执行的方法(与WaitCallback委托的声明必须一致)。
                // i   为传递给Fun方法的参数(obj将接受)。
            }

        }
        static int[] result = new int[10];
        //注意：由于WaitCallback委托的声明带有参数，
        //      所以将被调用的Fun方法必须带有参数，即：Fun(object obj)。
        static void Fun(object obj)
        {
            int n = (int)obj;        
            //计算阶乘
            int fac = 1;
            for (int i = 1; i <= n; i++)
            {
                fac *= i;
            }
            //保存结果
            result[n] = fac;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            TaskCancel();
        }

        private void _WebView(object e)
        {
            _mre.WaitOne();// http://www.cnblogs.com/tianzhiliang/archive/2011/03/04/1970726.html
            WebBrowser _web_e = (WebBrowser)e;
            //http://www.xjflcp.com/ssc/
       
        }

        private void Web_LoadCompleted(object sender, NavigationEventArgs e)
        {
            WebBrowser web = (WebBrowser)sender;
            //HTMLDocument htmldocument = (HTMLDocument)web.Document;         
            mshtml.HTMLDocument dom = (mshtml.HTMLDocument)web.Document; //定义HTML
            //dom.documentElement.style.overflow = "hidden"; //隐藏浏览器的滚动条
            //dom.body.setAttribute("scroll", "no"); //禁用浏览器的滚动条
            dom.getElementById("inputText").innerText = "Исключение, которое выбрасывается, в случае возникновения ошибки при работе с сетью.";
            dom.getElementById("translateBtn").click();
            task2.Wait(1000);
            m_SyncContext.Post(lable1, "task2 执行ing" + ((int)(stopwatch.ElapsedMilliseconds)).ToString());
            string result = dom.getElementById("translateBtn").outerHTML.ToString();

            httpListenner(dom);






            //   var  ElementCollection =( (HTMLDocument)web.Document).getElementsByTagName("Table");
            //foreach (  item in ElementCollection)
            //{
            //    File.AppendAllText("Kaijiang_xj.txt", item.InnerText);
            //}
        }

        private static void httpListenner(HTMLDocument dom)
        {
            //监听返回的数据

            HttpListener listenner = new HttpListener();
            listenner.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            listenner.Prefixes.Add(dom.url);
            listenner.Start();
            new Thread(new ThreadStart(delegate
            {
                while (true)
                {

                    HttpListenerContext httpListenerContext = listenner.GetContext();
                    httpListenerContext.Response.StatusCode = 200;
                    using (StreamWriter writer = new StreamWriter(httpListenerContext.Response.OutputStream))
                    {
                        //writer.WriteLine("<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><title>测试服务器</title></head><body>");
                        //writer.WriteLine("<div style=\"height:20px;color:blue;text-align:center;\"><p> hello</p></div>");
                        //writer.WriteLine("<ul>");
                        //writer.WriteLine("</ul>");
                        //writer.WriteLine("</body></html>");

                    }

                }
            })).Start();
        }


        //cancel
        private void TaskCancel()
        {
            tokenSource.Cancel();
        }
        //暂停


        //等待完成执行下一步

        //UI
        private void lable1(object str)
        {
            this.label.Content = (string)str;
        }

        private void webView_LoadCompleted(object sender, NavigationEventArgs e)
        {


            WebBrowser web = (WebBrowser)sender;
            //HTMLDocument htmldocument = (HTMLDocument)web.Document;         
            mshtml.HTMLDocument dom = (mshtml.HTMLDocument)web.Document; //定义HTML
            //dom.documentElement.style.overflow = "hidden"; //隐藏浏览器的滚动条
            //dom.body.setAttribute("scroll", "no"); //禁用浏览器的滚动条
            dom.getElementById("inputText").innerText = "Исключение, которое выбрасывается, в случае возникновения ошибки при работе с сетью.";
            dom.getElementById("translateBtn").click();

            //task2.Wait(1000);
            Thread.Sleep(1000);

          var s=   web.DataContext;
            string result = dom.getElementById("translateBtn").outerHTML.ToString();
            httpListenner(dom);

            //webView_DataContextChanged  
            //if (!dom.body.innerHTML.Contains("123456"))
            //{
            //    string szTmp = "http://192.168.0.11/sample2.htm";
            //    Uri uri = new Uri(szTmp);
            //    CamWeb.Navigate(uri);
            //}

        }


        private void webView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser web1 = (WebBrowser)sender;
            mshtml.HTMLDocument dom2 = (mshtml.HTMLDocument)web1.Document; //定义HTML
            string result = dom2.getElementById("outputText").outerText;
            IHTMLElement element = dom2.getElementById("outputText");
        }




    }
}
