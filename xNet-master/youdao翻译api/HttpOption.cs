using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace youdao翻译api
{
    // 与指定URL创建HTTP请求
    //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
    //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0; BOIE9;ZHCN)";
    //    request.Method = "GET";
    //    request.Accept = "*/*";
    //    //如果方法验证网页来源就加上这一句如果不验证那就可以不写了
    //    request.Referer = "http://txw1958.cnblogs.com";
    //    CookieContainer objcok = new CookieContainer();
    //objcok.Add(new Uri("http://txw1958.cnblogs.com"), new Cookie("键", "值"));
    //    objcok.Add(new Uri("http://txw1958.cnblogs.com"), new Cookie("键", "值"));
    //    objcok.Add(new Uri("http://txw1958.cnblogs.com"), new Cookie("sidi_sessionid", "360A748941D055BEE8C960168C3D4233"));
    //    request.CookieContainer = objcok;
    //    //不保持连接
    //    request.KeepAlive = true;
    // 获取对应HTTP请求的响应



    [Serializable]
    public class HttpOption
    {
        private string _userAgent= "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0; BOIE9;ZHCN)";
        private string _accept="*/*";
        private string _method="Get";
        //如果方法验证网页来源就加上这一句如果不验证那就可以不写了
        private string _referer;
        private Cookie[] _cookies;
        private bool _keepAlive=false;


        public string Accept
        {
            get
            {
                return _accept;
            }

            set
            {
                _accept = value;
            }
        }

        public string UserAgent
        {
            get
            {
                return _userAgent;
            }

            set
            {
                _userAgent = value;
            }
        }

        public string Method
        {
            get
            {
                return _method;
            }

            set
            {
                _method = value;
            }
        }

        public Cookie[] Cookies
        {
            get
            {
                return _cookies;
            }

            set
            {
                _cookies = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return _keepAlive;
            }

            set
            {
                _keepAlive = value;
            }
        }

        public string Referer
        {
            get
            {
                return _referer;
            }

            set
            {
                _referer = value;
            }
        }


    }
}
