using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace WpfWebdown.Com
{
    public class HttpWebclient
    {
        //https  ssl x509证书全部通过
        public bool httpsCertificateValidationCallback(object Sender, X509Certificate x509cc, X509Chain x509cn, SslPolicyErrors erro)
        {
            return true;
        }

        public static CookieContainer cooki_public = new CookieContainer();

        /// <summary>
        /// input  uri,encode,NetworkCredential(windows身份验证的名和密码) ;
        /// ///output  Return  web str  ;
        /// </summary>
        #region GetWeb
        HttpWebRequest webRequestget;
        /// <summary>
        ///output  web str  
        /// input  uri,encode,NetworkCredential(windows身份验证)
        /// </summary>
        /// <param name="uri"></param>
        ///  <param name="httpheader">httpoption对象包含cookis等信息 如果存在</param>
        /// <param name="encode">如果不存在给默认值</param>
        /// <param name="networkCredentials"> 如果存在，就赋给当前连接</param>
        /// <returns></returns>
        public string GetWeb(string uri, HttpOption httpheader = null, Encoding encode = null, NetworkCredential networkCredentials = null)
        {

            if (networkCredentials != null)
            { webRequestget.Credentials = networkCredentials; }
            if (encode == null)
            { encode = Encoding.UTF8; }
            if (uri.Substring(0, 5).ToLower().Equals("https"))
            {
                ////这一句一定要写在创建连接的前面。使用回调的方法进行证书验证。
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(httpsCertificateValidationCallback);
                webRequestget = (HttpWebRequest)HttpWebRequest.Create(uri);
                //将取消验证的证书保存到本地，访问web和post数据时候会用到                
                X509Certificate x509cer = new X509Certificate(AppDomain.CurrentDomain.BaseDirectory + "\\sslcard.cer");
                webRequestget.ClientCertificates.Add(x509cer);//添加到请求对象中
            }
            else
            {
                webRequestget = (HttpWebRequest)HttpWebRequest.Create(uri);
            
            }
            //cookir如果报文头存在则赋给相关对象
            if (httpheader != null)
            {
                webRequestget.Accept = httpheader.Accept;
                webRequestget.Referer = httpheader.Referer;
                webRequestget.Method = httpheader.Method;
                webRequestget.KeepAlive = httpheader.KeepAlive;
                webRequestget.UserAgent = httpheader.UserAgent;
                if (httpheader.Cookies.Length > 0)
                {
                    CookieContainer cookies = new CookieContainer();
                    foreach (var item in httpheader.Cookies)
                    {
                        cookies.Add(item);
                    }
                    webRequestget.CookieContainer = cookies;
                }
            }

            return _webGet(uri, encode);
        }
        //get data
        private string _webGet(string uri, Encoding encode)
        {
            string webdownstr = string.Empty;
            try
            {
                using (WebResponse webResponse = webRequestget.GetResponse())
                {

                    using (System.IO.StreamReader streamreader = new System.IO.StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        webdownstr = streamreader.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
            {
                webdownstr = string.Format("{0}:{1}", e.Status, e.InnerException.Message);
            }

            return webdownstr;
        }
        #endregion

        /// <summary>
        /// input  uri,postdata,httpheader, encode 默认utf-8,NetworkCredential(windows身份验证的名和密码) ;
        /// ///output  Return  web str  ;
        /// </summary>
        #region Webpost        
        HttpWebRequest webRequestpost;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="postdata"></param>
        /// <param name="encoding"></param>
        /// <param name="httpoption"></param>
        /// <param name="networkcredential"></param>
        /// <returns></returns>
        public string PostWeb(string uri, string postdata, Encoding encoding = null, HttpOption httpheader = null, NetworkCredential networkcredential = null)
        {
            return _Post2Web(uri, postdata, ref encoding, httpheader, networkcredential);
        }

        private string _Post2Web(string uri, string postdata, ref Encoding encoding, HttpOption httpheader, NetworkCredential networkcredential)
        {
            string webdata = "";
            //安全证书  ssl
            if (uri.Substring(0, 5).ToLower().Equals("https"))
            {
                //使用回调的证书
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(httpsCertificateValidationCallback);
                webRequestpost = (HttpWebRequest)HttpWebRequest.Create(uri);
                X509Certificate x509cer = new X509Certificate(AppDomain.CurrentDomain.BaseDirectory + "postssl.cer");
                webRequestpost.ClientCertificates.Add(x509cer);
            }
            else
            {
                webRequestpost = (HttpWebRequest)HttpWebRequest.Create(uri);
            }
            if (networkcredential != null)
            {
                webRequestpost.Credentials = networkcredential;
            }
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            if (httpheader != null)
            {
                webRequestpost.Accept = httpheader.Accept;
                webRequestpost.Referer = httpheader.Referer;
                webRequestpost.Method = httpheader.Method;
                webRequestpost.KeepAlive = httpheader.KeepAlive;
                webRequestpost.UserAgent = httpheader.UserAgent;
                if (httpheader.Cookies.Length > 0)
                {
                    CookieContainer cookies = new CookieContainer();
                    //for (int i = 0; i < httpheader.Cookies.Length; i++)
                    //{
                    // httpheader.Cookies[i]);
                    //}
                    if (cooki_public != null && cooki_public.Count > 0)
                    {
                        cookies = cooki_public;
                    }
                    foreach (Cookie item in httpheader.Cookies)
                    {

                        //cookies.Add(item);
                    }
                    webRequestpost.CookieContainer = cookies;
                }
            }


            try
            {
                webRequestpost.Method = "Post";
                byte[] buffer = encoding.GetBytes(postdata);
                webRequestpost.ContentLength = buffer.Length;
                webRequestpost.GetRequestStream().Write(buffer, 0, buffer.Length);

                using (HttpWebResponse httpwebresponse = (HttpWebResponse)webRequestpost.GetResponse())
                {
                    cooki_public = webRequestpost.CookieContainer;
                    using (System.IO.StreamReader streamreader = new System.IO.StreamReader(httpwebresponse.GetResponseStream()))
                    {
                        webdata = streamreader.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
            {
                webdata = string.Format("{0}:{1}", e.Status, e.InnerException.Message);
            }
            return webdata;
        }
        #endregion


    }
}
