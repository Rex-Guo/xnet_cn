using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// 是предназначенн类发送请求的HTTP服务器。
    /// </summary>
    public class HttpRequest : IDisposable
    {
        // 用于确定发送多少字节/认为。
        private sealed class HttpWraperStream : Stream
        {
            #region 地板(关闭)

            private Stream _baseStream;
            private int _sendBufferSize;

            #endregion


            #region 性能(开放)

            public Action<int> BytesReadCallback { get; set; }

            public Action<int> BytesWriteCallback { get; set; }

            #region 超定

            public override bool CanRead
            {
                get
                {
                    return _baseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return _baseStream.CanSeek;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    return _baseStream.CanTimeout;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return _baseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return _baseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _baseStream.Position;
                }
                set
                {
                    _baseStream.Position = value;
                }
            }

            #endregion

            #endregion


            public HttpWraperStream(Stream baseStream, int sendBufferSize)
            {
                _baseStream = baseStream;
                _sendBufferSize = sendBufferSize;
            }


            #region 方法(开放)

            public override void Flush() { }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = _baseStream.Read(buffer, offset, count);

                if (BytesReadCallback != null)
                {
                    BytesReadCallback(bytesRead);
                }

                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (BytesWriteCallback == null)
                {
                    _baseStream.Write(buffer, offset, count);
                }
                else
                {
                    int index = 0;

                    while (count > 0)
                    {
                        int bytesWrite = 0;

                        if (count >= _sendBufferSize)
                        {
                            bytesWrite = _sendBufferSize;
                            _baseStream.Write(buffer, index, bytesWrite);

                            index += _sendBufferSize;
                            count -= _sendBufferSize;
                        }
                        else
                        {
                            bytesWrite = count;
                            _baseStream.Write(buffer, index, bytesWrite);

                            count = 0;
                        }

                        BytesWriteCallback(bytesWrite);
                    }
                }
            }

            #endregion
        }


        /// <summary>
        /// 版本的HTTP请求中使用。
        /// </summary>
        public static readonly Version ProtocolVersion = new Version(1, 1);


        #region 静电场(关闭)

        // 标题,您只能通过特殊性能/方法。
        private static readonly List<string> _closedHeaders = new List<string>()
        {
            "Accept-Encoding",
            "Content-Length",
            "Content-Type",
            "Connection",
            "Proxy-Connection",
            "Host"
        };

        #endregion


        #region 静态特性(开放)

        /// <summary>
        /// 返回或设置为指定是否使用Internet Explorer的代理客户'a, 如果没有直接连接到互联网和客户没有指定代理。
        /// </summary>
        /// <value>默认值— <see langword="false"/>.</value>
        public static bool UseIeProxy { get; set; }

        /// <summary>
        /// 返回或设置为指定是否要禁用代理客户机本地地址。
        /// </summary>
        /// <value>默认值— <see langword="false"/>.</value>
        public static bool DisableProxyForLocalAddress { get; set; }

        /// <summary>
        /// 返回指定的代理或全球客户。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public static ProxyClient GlobalProxy { get; set; }

        #endregion


        #region 地板(关闭)

        private HttpResponse _response;

        private TcpClient _connection;
        private Stream _connectionCommonStream;
        private NetworkStream _connectionNetworkStream;

        private ProxyClient _currentProxy;

        private int _redirectionCount = 0;
        private int _maximumAutomaticRedirections = 5;

        private int _connectTimeout = 60 * 1000;
        private int _readWriteTimeout = 60 * 1000;

        private DateTime _whenConnectionIdle;
        private int _keepAliveTimeout = 30 * 1000;
        private int _maximumKeepAliveRequests = 100;
        private int _keepAliveRequestCount;
        private bool _keepAliveReconnected;

        private int _reconnectLimit = 3;
        private int _reconnectDelay = 100;
        private int _reconnectCount;

        private HttpMethod _method;
        private HttpContent _content; // 体请求。

        private readonly Dictionary<string, string> _permanentHeaders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 临时数据,提出通过特殊方法。
        // 先删除请求。
        private RequestParams _temporaryParams;
        private RequestParams _temporaryUrlParams;
        private Dictionary<string, string> _temporaryHeaders;
        private MultipartContent _temporaryMultipartContent;

        // 发送和接受数量的字节。
        // Используютсядлясобытийи。UploadProgressChanged DownloadProgressChanged
        private long _bytesSent;
        private long _totalBytesSent;
        private long _bytesReceived;
        private long _totalBytesReceived;
        private bool _canReportBytesReceived;

        private EventHandler<UploadProgressChangedEventArgs> _uploadProgressChangedHandler;
        private EventHandler<DownloadProgressChangedEventArgs> _downloadProgressChangedHandler;


        #endregion


        #region 事件(开放)

        /// <summary>
        /// 每次遇到数据卸载期间促销消息体。
        /// </summary>
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged
        {
            add
            {
                _uploadProgressChangedHandler += value;
            }
            remove
            {
                _uploadProgressChangedHandler -= value;
            }
        }

        /// <summary>
        /// 每次出现下载进度推进数据消息体。
        /// </summary>
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged
        {
            add
            {
                _downloadProgressChangedHandler += value;
            }
            remove
            {
                _downloadProgressChangedHandler -= value;
            }
        }

        #endregion


        #region 性能(开放)

        /// <summary>
        /// 返回指定URI或使用互联网资源,如果查询指定相对地址。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// 互联网资源URI返回实际回答查询。
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// 最后返回HTTP服务器响应数据的类的实例。
        /// </summary>
        public HttpResponse Response
        {
            get
            {
                return _response;
            }
        }

        /// <summary>
        /// 返回指定代理或客户。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public ProxyClient Proxy { get; set; }

        /// <summary>
        /// 返回指定代表或者方法调用验证用于验证SSL证书的真实性。
        /// </summary>
        /// <value>默认值— <see langword="null"/>. 如果设置默认值,则使用方法都接受SSL证书。</value>
        public RemoteCertificateValidationCallback SslCertificateValidatorCallback;

        #region 行为

        /// <summary>
        /// 或返回值指定是否应该遵循查询答案转发。
        /// </summary>
        /// <value>默认值— <see langword="true"/>.</value>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        /// 返回给定序列最大值或转发。
        /// </summary>
        /// <value>默认值5.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小1.</exception>
        public int MaximumAutomaticRedirections
        {
            get
            {
                return _maximumAutomaticRedirections;
            }
            set
            {
                #region 检查参数

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumAutomaticRedirections", 1);
                }

                #endregion

                _maximumAutomaticRedirections = value;
            }
        }

        /// <summary>
        /// 返回指定的毫秒或等待时间连接到HTTP服务器。
        /// </summary>
        /// <value>默认值60.000, 相当于一分钟。</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小0.</exception>
        public int ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }
            set
            {
                #region 检查参数

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ConnectTimeout", 0);
                }

                #endregion

                _connectTimeout = value;
            }
        }

        /// <summary>
        /// 返回指定的毫秒或等待时间记录或读取流中的他。
        /// </summary>
        /// <value>默认值60.000, 相当于一分钟。</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小0.</exception>
        public int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                #region 检查参数

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReadWriteTimeout", 0);
                }

                #endregion

                _readWriteTimeout = value;
            }
        }

        /// <summary>
        /// 或返回值指定是否忽略错误记录和不产生异常。
        /// </summary>
        /// <value>默认值— <see langword="false"/>.</value>
        /// <remarks>如果设置值<see langword="true"/>, 在收到错误代码回复状态4xx 或5xx, 会生成异常。你可以了解通过属性状态码答<see cref="HttpResponse.StatusCode"/>.</remarks>
        public bool IgnoreProtocolErrors { get; set; }

        /// <summary>
        /// 或返回值指定是否需要设置固定接入互联网资源。
        /// </summary>
        /// <value>默认值<see langword="true"/>.</value>
        /// <remarks>如果值相等<see langword="true"/>, 发送更多的话题'Connection: Keep-Alive', 否则发送标题'Connection: Close'. 如果使用HTTP代理连接一起,标题-'Connection', 设置标题-'Proxy-Connection'. 如果服务器连接,不断打破皮<see cref="HttpResponse"/> 尝试重新连接,但它只有连接来直接与HTTP服务器或HTTP代理。</remarks>
        public bool KeepAlive { get; set; }

        /// <summary>
        /// 返回给定时间闲置或固定连接的默认使用毫秒。
        /// </summary>
        /// <value>默认值30.000, 相当于30 秒。</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小0.</exception>
        /// <remarks>如果时间到了,就会创建一个新的连接。如果服务器超时的值会<see cref="HttpResponse.KeepAliveTimeout"/>, 然后将他名字。</remarks>
        public int KeepAliveTimeout
        {
            get
            {
                return _keepAliveTimeout;
            }
            set
            {
                #region 检查参数

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("KeepAliveTimeout", 0);
                }

                #endregion

                _keepAliveTimeout = value;
            }
        }

        /// <summary>
        /// 返回给定数量或最大允许连接查询,默认使用。
        /// </summary>
        /// <value>默认值100.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小1.</exception>
        /// <remarks>如果数量超过最大请求,则会创建新的连接。如果服务器回来的值最大可查询哇<see cref="HttpResponse.MaximumKeepAliveRequests"/>, 然后将他名字。</remarks>
        public int MaximumKeepAliveRequests
        {
            get
            {
                return _maximumKeepAliveRequests;
            }
            set
            {
                #region 检查参数

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("MaximumKeepAliveRequests", 1);
                }

                #endregion

                _maximumKeepAliveRequests = value;
            }
        }

        /// <summary>
        /// 或返回值指定是否要尝试重新连接通过n毫秒,如果发生错误或连接时发送/下载数据。
        /// </summary>
        /// <value>默认值<see langword="false"/>.</value>
        public bool Reconnect { get; set; }

        /// <summary>
        /// 返回指定最大值或尝试重新连接。
        /// </summary>
        /// <value>默认值3.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小1.</exception>
        public int ReconnectLimit
        {
            get
            {
                return _reconnectLimit;
            }
            set
            {
                #region 检查参数

                if (value < 1)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectLimit", 1);
                }

                #endregion

                _reconnectLimit = value;
            }
        }

        /// <summary>
        /// 返回给定延迟毫秒或产生之前,重新执行。
        /// </summary>
        /// <value>默认值100 毫秒。</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小0.</exception>
        public int ReconnectDelay
        {
            get
            {
                return _reconnectDelay;
            }
            set
            {
                #region 检查参数

                if (value < 0)
                {
                    throw ExceptionHelper.CanNotBeLess("ReconnectDelay", 0);
                }

                #endregion

                _reconnectDelay = value;
            }
        }

        #endregion

        #region HTTP-标题

        /// <summary>
        /// 语言用于日常查询。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <remarks>如果语言设置,则额外发送标题'Accept-Language' 这些语言名称。</remarks>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// 返回指定或编码,用于出站和入站数据转换。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <remarks>如果编码设置,则额外发送标题'Accept-Charset' 这些编码名称,但是如果这个标题不是直接问。定义自动编码,但回答她,如果不能确定,则将数据值的属性。如果没有指定此属性值,则将使用值<see cref="System.Text.Encoding.Default"/>.</remarks>
        public Encoding CharacterSet { get; set; }

        /// <summary>
        /// 或返回值指定是否要编码的内容回答。首先是利用压缩数据。
        /// </summary>
        /// <value>默认值<see langword="true"/>.</value>
        /// <remarks>如果值相等<see langword="true"/>, 发送更多的话题'Accept-Encoding: gzip, deflate'.</remarks>
        public bool EnableEncodingContent { get; set; }

        /// <summary>
        /// 返回指定或授权用户的基本HTTP服务器。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <remarks>如果设置值,则额外发送标题'Authorization'.</remarks>
        public string Username { get; set; }

        /// <summary>
        /// 返回指定或授权密码基本的HTTP服务器。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <remarks>如果设置值,则额外发送标题'Authorization'.</remarks>
        public string Password { get; set; }

        /// <summary>
        /// 返回值或HTTP报头'User-Agent'.
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public string UserAgent
        {
            get
            {
                return this["User-Agent"];
            }
            set
            {
                this["User-Agent"] = value;
            }
        }

        /// <summary>
        /// 返回值或HTTP报头'Referer'.
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public string Referer
        {
            get
            {
                return this["Referer"];
            }
            set
            {
                this["Referer"] = value;
            }
        }

        /// <summary>
        /// 返回值或HTTP报头'Authorization'.
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        public string Authorization
        {
            get
            {
                return this["Authorization"];
            }
            set
            {
                this["Authorization"] = value;
            }
        }

        /// <summary>
        /// 返回cookie或指定相关查询。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <remarks>厨师可以修改答案从HTTP服务器。为了避免这些,需要设置属性<see cref="xNet.Net.CookieDictionary.IsLocked"/> 等于<see langword="true"/>.</remarks>
        public CookieDictionary Cookies { get; set; }

        #endregion

        #endregion


        #region 性能(内部)

        internal TcpClient TcpClient
        {
            get
            {
                return _connection;
            }
        }

        internal Stream ClientStream
        {
            get
            {
                return _connectionCommonStream;
            }
        }

        internal NetworkStream ClientNetworkStream
        {
            get
            {
                return _connectionNetworkStream;
            }
        }

        #endregion


        private MultipartContent AddedMultipartData
        {
            get
            {
                if (_temporaryMultipartContent == null)
                {
                    _temporaryMultipartContent = new MultipartContent();
                }

                return _temporaryMultipartContent;
            }
        }


        #region 索引器(开放)

        /// <summary>
        /// 返回或设置为HTTP报头。
        /// </summary>
        /// <param name="headerName">HTTP报头名称。</param>
        /// <value>HTTP报头的值,如果他问,否则空字符串。如果给定值<see langword="null"/> 或空字符串,那么将远程HTTP标题的列表。</value>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="headerName"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="headerName"/> 是空字符串。
        /// -或-
        /// 设置HTTP报头值必须用特殊性能问/方法。
        /// </exception>
        /// <remarks>HTTP报头的名单必须用专用属性只问/方法:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public string this[string headerName]
        {
            get
            {
                #region 检查参数

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                #endregion

                string value;

                if (!_permanentHeaders.TryGetValue(headerName, out value))
                {
                    value = string.Empty;
                }

                return value;
            }
            set
            {
                #region 检查参数

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                if (IsClosedHeader(headerName))
                {
                    throw new ArgumentException(string.Format(
                        Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, headerName), "headerName");
                }

                #endregion

                if (string.IsNullOrEmpty(value))
                {
                    _permanentHeaders.Remove(headerName);
                }
                else
                {
                    _permanentHeaders[headerName] = value;
                }
            }
        }

        /// <summary>
        /// 返回或设置为HTTP报头。
        /// </summary>
        /// <param name="header">HTTP-标题。</param>
        /// <value>HTTP报头的值,如果他问,否则空字符串。如果给定值<see langword="null"/> 或空字符串,那么将远程HTTP标题的列表。</value>
        /// <exception cref="System.ArgumentException">设置HTTP报头值必须用特殊性能问/方法。</exception>
        /// <remarks>HTTP报头的名单必须用专用属性只问/方法:
        /// <list type="table">
        ///     <item>
        ///        <description>Accept-Encoding</description>
        ///     </item>
        ///     <item>
        ///        <description>Content-Length</description>
        ///     </item>
        ///     <item>
        ///         <description>Content-Type</description>
        ///     </item>
        ///     <item>
        ///        <description>Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Proxy-Connection</description>
        ///     </item>
        ///     <item>
        ///        <description>Host</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public string this[HttpHeader header]
        {
            get
            {
                return this[Http.Headers[header]];
            }
            set
            {
                this[Http.Headers[header]] = value;
            }
        }

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest()
        {
            Init();
        }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">互联网地址资源,如果使用查询指定相对地址。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="baseAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="baseAddress"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="baseAddress"/> 并非绝对URI。
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="baseAddress"/> 并非绝对URI。</exception>
        public HttpRequest(string baseAddress)
        {
            #region 检查参数

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (baseAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("baseAddress");
            }

            #endregion

            if (!baseAddress.StartsWith("http"))
            {
                baseAddress = "http://" + baseAddress;
            }

            var uri = new Uri(baseAddress);

            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            BaseAddress = uri;

            Init();
        }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="baseAddress">互联网地址资源,如果使用查询指定相对地址。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="baseAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="baseAddress"/> 并非绝对URI。</exception>
        public HttpRequest(Uri baseAddress)
        {
            #region 检查参数

            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException(Resources.ArgumentException_OnlyAbsoluteUri, "baseAddress");
            }

            #endregion

            BaseAddress = baseAddress;

            Init();
        }

        #endregion


        #region 方法(开放)

        #region Get

        /// <summary>
        /// 发送服务器的HTTP GET请求。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="urlParams">参数值或URL<see langword="null"/>.</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="address"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Get(string address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        /// <summary>
        /// 发送服务器的HTTP GET请求。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="urlParams">参数值或URL<see langword="null"/>.</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Get(Uri address, RequestParams urlParams = null)
        {
            if (urlParams != null)
            {
                _temporaryUrlParams = urlParams;
            }

            return Raw(HttpMethod.GET, address);
        }

        #endregion

        #region Post

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="address"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address)
        {
            return Raw(HttpMethod.POST, address);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="reqParams">查询参数,发送HTTP服务器。</param>
        /// <param name="dontEscape">是否需要指定请求参数编码。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="reqParams"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="address"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, RequestParams reqParams, bool dontEscape = false)
        {
            #region 检查参数

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="reqParams">查询参数,发送HTTP服务器。</param>
        /// <param name="dontEscape">是否需要指定请求参数编码。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="reqParams"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, RequestParams reqParams, bool dontEscape = false)
        {
            #region 检查参数

            if (reqParams == null)
            {
                throw new ArgumentNullException("reqParams");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FormUrlEncodedContent(reqParams, dontEscape, CharacterSet));
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="str">行,发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="str"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="address"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="str"/> 是空字符串。
        /// -或
        /// 参数值<paramref name="contentType"/> 是空字符串。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, string str, string contentType)
        {
            #region 检查参数

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="str">行,发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="str"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="str"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="contentType"/> 是空字符串。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, string str, string contentType)
        {
            #region 检查参数

            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (str.Length == 0)
            {
                throw new ArgumentNullException("str");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StringContent(str)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="bytes">字节数组,发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="bytes"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="address"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="contentType"/> 是空字符串。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region 检查参数

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="bytes">字节数组,发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="bytes"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="contentType"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, byte[] bytes, string contentType = "application/octet-stream")
        {
            #region 检查参数

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new BytesContent(bytes)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="stream">数据流发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="stream"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="address"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="contentType"/> 是空字符串。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, Stream stream, string contentType = "application/octet-stream")
        {
            #region 检查参数

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="stream">数据流发送HTTP服务器。</param>
        /// <param name="contentType">式发送数据。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="stream"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="contentType"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, Stream stream, string contentType = "application/octet-stream")
        {
            #region 检查参数

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (contentType.Length == 0)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            var content = new StreamContent(stream)
            {
                ContentType = contentType
            };

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="path">路径将数据发送的HTTP服务器。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="path"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="address"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="path"/> 是空字符串。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, string path)
        {
            #region 检查参数

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="path">路径将数据发送的HTTP服务器。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="path"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="path"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, string path)
        {
            #region 检查参数

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            return Raw(HttpMethod.POST, address, new FileContent(path));
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="content">发送内容HTTP服务器。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="address"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(string address, HttpContent content)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }

        /// <summary>
        /// 发送POST请求的HTTP服务器。
        /// </summary>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="content">发送内容HTTP服务器。</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="address"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Post(Uri address, HttpContent content)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            return Raw(HttpMethod.POST, address, content);
        }

        #endregion

        #region Raw

        /// <summary>
        /// 发送请求的HTTP服务器。
        /// </summary>
        /// <param name="method">HTTP-查询方法。</param>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="content">发送内容HTTP服务器或意义<see langword="null"/>.</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="address"/> 是空字符串。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Raw(HttpMethod method, string address, HttpContent content = null)
        {
            #region 检查参数

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            if (address.Length == 0)
            {
                throw ExceptionHelper.EmptyString("address");
            }

            #endregion

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);
            return Raw(method, uri, content);
        }

        /// <summary>
        /// 发送请求的HTTP服务器。
        /// </summary>
        /// <param name="method">HTTP-查询方法。</param>
        /// <param name="address">互联网地址资源。</param>
        /// <param name="content">发送内容HTTP服务器或意义<see langword="null"/>.</param>
        /// <returns>用于装载对象响应HTTP服务器。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="address"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public HttpResponse Raw(HttpMethod method, Uri address, HttpContent content = null)
        {
            #region 检查参数

            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            #endregion

            if (!address.IsAbsoluteUri)
                address = GetRequestAddress(BaseAddress, address);

            if (_temporaryUrlParams != null)
            {
                var uriBuilder = new UriBuilder(address);
                uriBuilder.Query = Http.ToQueryString(_temporaryUrlParams, true);

                address = uriBuilder.Uri;
            }

            if (content == null)
            {
                if (_temporaryParams != null)
                {
                    content = new FormUrlEncodedContent(_temporaryParams, false, CharacterSet);
                }
                else if (_temporaryMultipartContent != null)
                {
                    content = _temporaryMultipartContent;
                }
            }

            try
            {
                return Request(method, address, content);
            }
            finally
            {
                if (content != null)
                    content.Dispose();

                ClearRequestData();
            }
        }

        #endregion

        #region 添加数据查询时间

        /// <summary>
        /// 添加时间参数的URL。
        /// </summary>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值或值<see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="name"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>这个参数将стёрт第一后查询。</remarks>
        public HttpRequest AddUrlParam(string name, object value = null)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryUrlParams == null)
            {
                _temporaryUrlParams = new RequestParams();
            }

            _temporaryUrlParams[name] = value;

            return this;
        }

        /// <summary>
        /// 添加时间查询参数。
        /// </summary>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值或值<see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="name"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>这个参数将стёрт第一后查询。</remarks>
        public HttpRequest AddParam(string name, object value = null)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            if (_temporaryParams == null)
            {
                _temporaryParams = new RequestParams();
            }

            _temporaryParams[name] = value;

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="value">元素值或值<see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="name"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddField(string name, object value = null)
        {
            return AddField(name, value, CharacterSet ?? Encoding.UTF8);
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="value">元素值或值<see langword="null"/>.</param>
        /// <param name="encoding">采用编码转换为字节序列中的值。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="encoding"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddField(string name, object value, Encoding encoding)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            #endregion

            string contentValue = (value == null ? string.Empty : value.ToString());

            AddedMultipartData.Add(new StringContent(contentValue, encoding), name);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="value">元素值。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="value"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddField(string name, byte[] value)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据文件构成。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">他们传递文件。</param>
        /// <param name="value">数据文件。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="value"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddFile(string name, string fileName, byte[] value)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据文件构成。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">他们传递文件。</param>
        /// <param name="contentType">MIME-内容类型。</param>
        /// <param name="value">数据文件。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="value"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddFile(string name, string fileName, string contentType, byte[] value)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            AddedMultipartData.Add(new BytesContent(value), name, fileName, contentType);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据文件构成。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">他们传递文件。</param>
        /// <param name="stream">数据流文件。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="stream"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddFile(string name, string fileName, Stream stream)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            AddedMultipartData.Add(new StreamContent(stream), name, fileName);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据文件构成。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">他们传递文件。</param>
        /// <param name="path">上传文件的路径。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="path"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="name"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="path"/> 是空字符串。
        /// </exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddFile(string name, string fileName, string path)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path), name, fileName);

            return this;
        }

        /// <summary>
        /// 添加时间元素Multipart/form 数据文件构成。
        /// </summary>
        /// <param name="name">元素名称。</param>
        /// <param name="path">上传文件的路径。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="path"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="name"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="path"/> 是空字符串。
        /// </exception>
        /// <remarks>数据元素是стёрт第一后查询。</remarks>
        public HttpRequest AddFile(string name, string path)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw ExceptionHelper.EmptyString("path");
            }

            #endregion

            AddedMultipartData.Add(new FileContent(path),
                name, Path.GetFileName(path));

            return this;
        }

        /// <summary>
        /// 临时增加的HTTP标题查询。这种标题标题索引器通过建立重叠。
        /// </summary>
        /// <param name="name">HTTP报头名称。</param>
        /// <param name="value">HTTP报头的值。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="value"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="name"/> 是空字符串。
        /// -或-
        /// 参数值<paramref name="value"/> 是空字符串。
        /// -或-
        /// 设置HTTP报头值必须用特殊性能问/方法。
        /// </exception>
        /// <remarks>数据将стёртHTTP标题后第一个请求。</remarks>
        public HttpRequest AddHeader(string name, string value)
        {
            #region 检查参数

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length == 0)
            {
                throw ExceptionHelper.EmptyString("value");
            }

            if (IsClosedHeader(name))
            {
                throw new ArgumentException(string.Format(
                    Resources.ArgumentException_HttpRequest_SetNotAvailableHeader, name), "name");
            }

            #endregion

            if (_temporaryHeaders == null)
            {
                _temporaryHeaders = new Dictionary<string, string>();
            }

            _temporaryHeaders[name] = value;

            return this;
        }

        /// <summary>
        /// 临时增加的HTTP标题查询。这种标题标题索引器通过建立重叠。
        /// </summary>
        /// <param name="header">HTTP-标题。</param>
        /// <param name="value">HTTP报头的值。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="value"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">
        /// 参数值<paramref name="value"/> 是空字符串。
        /// -或-
        /// 设置HTTP报头值必须用特殊性能问/方法。
        /// </exception>
        /// <remarks>数据将стёртHTTP标题后第一个请求。</remarks>
        public HttpRequest AddHeader(HttpHeader header, string value)
        {
            AddHeader(Http.Headers[header], value);

            return this;
        }

        #endregion

        /// <summary>
        /// 从HTTP服务器关闭连接。
        /// </summary>
        /// <remarks>呼叫这个方法调用方法无异于<see cref="Dispose"/>.</remarks>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// 释放所有资源,使用当前类的实例<see cref="HttpRequest"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// 确定是否包含指定cookie。
        /// </summary>
        /// <param name="name">库克名称。</param>
        /// <returns>意义<see langword="true"/>, 如果库克所包含或意义<see langword="false"/>.</returns>
        public bool ContainsCookie(string name)
        {
            if (Cookies == null)
                return false;

            return Cookies.ContainsKey(name);
        }

        #region 标题的工作

        /// <summary>
        /// 确定是否包含指定的HTTP标题。
        /// </summary>
        /// <param name="headerName">HTTP报头名称。</param>
        /// <returns>意义<see langword="true"/>, 如果指定的HTTP标题包含值,否则<see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="headerName"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="headerName"/> 是空字符串。</exception>
        public bool ContainsHeader(string headerName)
        {
            #region 检查参数

            if (headerName == null)
            {
                throw new ArgumentNullException("headerName");
            }

            if (headerName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("headerName");
            }

            #endregion

            return _permanentHeaders.ContainsKey(headerName);
        }

        /// <summary>
        /// 确定是否包含指定的HTTP标题。
        /// </summary>
        /// <param name="header">HTTP-标题。</param>
        /// <returns>意义<see langword="true"/>, 如果指定的HTTP标题包含值,否则<see langword="false"/>.</returns>
        public bool ContainsHeader(HttpHeader header)
        {
            return ContainsHeader(Http.Headers[header]);
        }

        /// <summary>
        /// 返回的HTTP标题列出收藏。
        /// </summary>
        /// <returns>HTTP报头的收藏。</returns>
        public Dictionary<string, string>.Enumerator EnumerateHeaders()
        {
            return _permanentHeaders.GetEnumerator();
        }

        /// <summary>
        /// 清除所有HTTP报头。
        /// </summary>
        public void ClearAllHeaders()
        {
            _permanentHeaders.Clear();
        }

        #endregion

        #endregion


        #region 方法(焊)

        /// 免除失控(并可根据需要控制) 资源使用的对象<see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="disposing">意义<see langword="true"/> 允许和不可控释放资源; 意义<see langword="false"/> 只允许自由不羁的资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _connection != null)
            {
                _connection.Close();
                _connection = null;
                _connectionCommonStream = null;
                _connectionNetworkStream = null;

                _keepAliveRequestCount = 0;
            }
        }

        /// <summary>
        /// 事件引起<see cref="UploadProgressChanged"/>.
        /// </summary>
        /// <param name="e">事件参数。</param>
        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            EventHandler<UploadProgressChangedEventArgs> eventHandler = _uploadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// 事件引起<see cref="DownloadProgressChanged"/>.
        /// </summary>
        /// <param name="e">事件参数。</param>
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            EventHandler<DownloadProgressChangedEventArgs> eventHandler = _downloadProgressChangedHandler;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        #endregion


        #region 方法(关闭)

        private void Init()
        {
            KeepAlive = true;
            AllowAutoRedirect = true;
            EnableEncodingContent = true;

            _response = new HttpResponse(this);
        }

        private Uri GetRequestAddress(Uri baseAddress, Uri address)
        {
            var requestAddress = address;

            if (baseAddress == null)
            {
                var uriBuilder = new UriBuilder(address.OriginalString);
                requestAddress = uriBuilder.Uri;
            }
            else
            {
                Uri.TryCreate(baseAddress, address, out requestAddress);
            }

            return requestAddress;
        }

        #region 发送请求

        private HttpResponse Request(HttpMethod method, Uri address, HttpContent content)
        {
            _method = method;
            _content = content;

            CloseConnectionIfNeeded();

            var previousAddress = Address;
            Address = address;

            var createdNewConnection = false;
            try
            {
                createdNewConnection = TryCreateConnectionOrUseExisting(address, previousAddress);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw;
            }

            if (createdNewConnection)
                _keepAliveRequestCount = 1;
            else
                _keepAliveRequestCount++;

            #region 发送请求

            try
            {
                SendRequestData(method);
            }
            catch (SecurityException ex)
            {
                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }
            catch (IOException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                throw NewHttpException(Resources.HttpException_FailedSendRequest, ex, HttpExceptionStatus.SendFailure);
            }

            #endregion

            #region 响应头装

            try
            {
                ReceiveResponseHeaders(method);
            }
            catch (HttpException ex)
            {
                if (CanReconnect())
                    return ReconnectAfterFail();

                // 如果服务器断掉连接不断通过空连接,尝试重新回复。
                // 他能连接断掉,因为达到最大允许查询,或刚停机时间。
                if (KeepAlive && !_keepAliveReconnected && !createdNewConnection && ex.EmptyMessageBody)
                    return KeepAliveReconect();

                throw;
            }

            #endregion

            _response.ReconnectCount = _reconnectCount;

            _reconnectCount = 0;
            _keepAliveReconnected = false;
            _whenConnectionIdle = DateTime.Now;

            if (!IgnoreProtocolErrors)
                CheckStatusCode(_response.StatusCode);

            #region 转发

            if (AllowAutoRedirect && _response.HasRedirect)
            {
                if (++_redirectionCount > _maximumAutomaticRedirections)
                    throw NewHttpException(Resources.HttpException_LimitRedirections);

                ClearRequestData();
                return Request(HttpMethod.GET, _response.RedirectAddress, null);
            }

            _redirectionCount = 0;

            #endregion

            return _response;
        }

        private void CloseConnectionIfNeeded()
        {
            var hasConnection = (_connection != null);

            if (hasConnection && !_response.HasError &&
                !_response.MessageBodyLoaded)
            {
                try
                {
                    _response.None();
                }
                catch (HttpException)
                {
                    Dispose();
                }
            }
        }

        private bool TryCreateConnectionOrUseExisting(Uri address, Uri previousAddress)
        {
            ProxyClient proxy = GetProxy();

            var hasConnection = (_connection != null);
            var proxyChanged = (_currentProxy != proxy);

            var addressChanged =
                (previousAddress == null) ||
                (previousAddress.Port != address.Port) ||
                (previousAddress.Host != address.Host) ||
                (previousAddress.Scheme != address.Scheme);

            // 如果需要创建新的连接。
            if (!hasConnection || proxyChanged ||
                addressChanged || _response.HasError ||
                KeepAliveLimitIsReached())
            {
                _currentProxy = proxy;

                Dispose();
                CreateConnection(address);
                return true;
            }

            return false;
        }

        private bool KeepAliveLimitIsReached()
        {
            if (!KeepAlive)
                return false;

            var maximumKeepAliveRequests =
                _response.MaximumKeepAliveRequests ?? _maximumKeepAliveRequests;

            if (_keepAliveRequestCount >= maximumKeepAliveRequests)
                return true;

            var keepAliveTimeout =
                _response.KeepAliveTimeout ?? _keepAliveTimeout;

            var timeLimit = _whenConnectionIdle.AddMilliseconds(keepAliveTimeout);
            if (timeLimit < DateTime.Now)
                return true;

            return false;
        }

        private void SendRequestData(HttpMethod method)
        {
            var contentLength = 0L;
            var contentType = string.Empty;

            if (CanContainsRequestBody(method) && (_content != null))
            {
                contentType = _content.ContentType;
                contentLength = _content.CalculateContentLength();
            }

            var startingLine = GenerateStartingLine(method);
            var headers = GenerateHeaders(method, contentLength, contentType);

            var startingLineBytes = Encoding.ASCII.GetBytes(startingLine);
            var headersBytes = Encoding.ASCII.GetBytes(headers);

            _bytesSent = 0;
            _totalBytesSent = startingLineBytes.Length + headersBytes.Length + contentLength;

            _connectionCommonStream.Write(startingLineBytes, 0, startingLineBytes.Length);
            _connectionCommonStream.Write(headersBytes, 0, headersBytes.Length);

            var hasRequestBody = (_content != null) && (contentLength > 0);

            // 发送请求主体,如果他不存在。
            if (hasRequestBody)
                _content.WriteTo(_connectionCommonStream);
        }

        private void ReceiveResponseHeaders(HttpMethod method)
        {
            _canReportBytesReceived = false;

            _bytesReceived = 0;
            _totalBytesReceived = _response.LoadResponse(method);

            _canReportBytesReceived = true;
        }

        private bool CanReconnect()
        {
            return Reconnect && (_reconnectCount < _reconnectLimit);
        }

        private HttpResponse ReconnectAfterFail()
        {
            Dispose();
            Thread.Sleep(_reconnectDelay);

            _reconnectCount++;
            return Request(_method, Address, _content);
        }

        private HttpResponse KeepAliveReconect()
        {
            Dispose();
            _keepAliveReconnected = true;
            return Request(_method, Address, _content);
        }

        private void CheckStatusCode(HttpStatusCode statusCode)
        {
            var statusCodeNum = (int)statusCode;

            if ((statusCodeNum >= 400) && (statusCodeNum < 500))
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_ClientError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }

            if (statusCodeNum >= 500)
            {
                throw new HttpException(string.Format(
                    Resources.HttpException_SeverError, statusCodeNum),
                    HttpExceptionStatus.ProtocolError, _response.StatusCode);
            }
        }

        private bool CanContainsRequestBody(HttpMethod method)
        {
            return
                (method == HttpMethod.PUT) ||
                (method == HttpMethod.POST) ||
                (method == HttpMethod.DELETE);
        }

        #endregion

        #region 建立连接

        private ProxyClient GetProxy()
        {
            if (DisableProxyForLocalAddress)
            {
                try
                {
                    var checkIp = IPAddress.Parse("127.0.0.1");
                    IPAddress[] ips = Dns.GetHostAddresses(Address.Host);

                    foreach (var ip in ips)
                    {
                        if (ip.Equals(checkIp))
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw NewHttpException(
                            Resources.HttpException_FailedGetHostAddresses, ex);
                    }

                    throw;
                }
            }

            ProxyClient proxy = Proxy ?? GlobalProxy;

            if (proxy == null && UseIeProxy && !WinInet.InternetConnected)
            {
                proxy = WinInet.IEProxy;
            }

            return proxy;
        }

        private TcpClient CreateTcpConnection(string host, int port)
        {
            TcpClient tcpClient;

            if (_currentProxy == null)
            {
                #region 建立连接

                tcpClient = new TcpClient();

                Exception connectException = null;
                var connectDoneEvent = new ManualResetEventSlim();

                try
                {
                    tcpClient.BeginConnect(host, port, new AsyncCallback(
                        (ar) =>
                        {
                            try
                            {
                                tcpClient.EndConnect(ar);
                            }
                            catch (Exception ex)
                            {
                                connectException = ex;
                            }

                            connectDoneEvent.Set();
                        }), tcpClient
                    );
                }
                #region Catch's

                catch (Exception ex)
                {
                    tcpClient.Close();

                    if (ex is SocketException || ex is SecurityException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }

                #endregion

                if (!connectDoneEvent.Wait(_connectTimeout))
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_ConnectTimeout, null, HttpExceptionStatus.ConnectFailure);
                }

                if (connectException != null)
                {
                    tcpClient.Close();

                    if (connectException is SocketException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedConnect, connectException, HttpExceptionStatus.ConnectFailure);
                    }

                    throw connectException;
                }

                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    throw NewHttpException(Resources.HttpException_FailedConnect, null, HttpExceptionStatus.ConnectFailure);
                }

                #endregion

                tcpClient.SendTimeout = _readWriteTimeout;
                tcpClient.ReceiveTimeout = _readWriteTimeout;
            }
            else
            {
                try
                {
                    tcpClient = _currentProxy.CreateConnection(host, port);
                }
                catch (ProxyException ex)
                {
                    throw NewHttpException(Resources.HttpException_FailedConnect, ex, HttpExceptionStatus.ConnectFailure);
                }
            }

            return tcpClient;
        }

        private void CreateConnection(Uri address)
        {
            _connection = CreateTcpConnection(address.Host, address.Port);
            _connectionNetworkStream = _connection.GetStream();

            // 如果要求安全连接。
            if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SslStream sslStream;

                    if (SslCertificateValidatorCallback == null)
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, Http.AcceptAllCertificationsCallback);
                    }
                    else
                    {
                        sslStream = new SslStream(_connectionNetworkStream, false, SslCertificateValidatorCallback);
                    }

                    sslStream.AuthenticateAsClient(address.Host);
                    _connectionCommonStream = sslStream;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is AuthenticationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedSslConnect, ex, HttpExceptionStatus.ConnectFailure);
                    }

                    throw;
                }
            }
            else
            {
                _connectionCommonStream = _connectionNetworkStream;
            }

            if (_uploadProgressChangedHandler != null ||
                _downloadProgressChangedHandler != null)
            {
                var httpWraperStream = new HttpWraperStream(
                    _connectionCommonStream, _connection.SendBufferSize);

                if (_uploadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesWriteCallback = ReportBytesSent;
                }

                if (_downloadProgressChangedHandler != null)
                {
                    httpWraperStream.BytesReadCallback = ReportBytesReceived;
                }

                _connectionCommonStream = httpWraperStream;
            }
        }

        #endregion

        #region 建立数据查询

        private string GenerateStartingLine(HttpMethod method)
        {
            string query;

            if (_currentProxy != null &&
                (_currentProxy.Type == ProxyType.Http || _currentProxy.Type == ProxyType.Chain))
            {
                query = Address.AbsoluteUri;
            }
            else
            {
                query = Address.PathAndQuery;
            }

            return string.Format("{0} {1} HTTP/{2}\r\n",
                method, query, ProtocolVersion);
        }

        // 有3 标题类型可重叠。就是安装顺序:
        // - 提出通过标题或特殊性质自动
        // - 标题,通过给定索引器
        // - 时间,通过给定标题AddHeader法
        private string GenerateHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = GenerateCommonHeaders(method, contentLength, contentType);

            MergeHeaders(headers, _permanentHeaders);

            if (_temporaryHeaders != null && _temporaryHeaders.Count > 0)
                MergeHeaders(headers, _temporaryHeaders);

            if (Cookies != null && Cookies.Count != 0 && !headers.ContainsKey("Cookie"))
                headers["Cookie"] = Cookies.ToString();

            return ToHeadersString(headers);
        }

        private Dictionary<string, string> GenerateCommonHeaders(HttpMethod method, long contentLength = 0, string contentType = null)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            #region Host

            if (Address.IsDefaultPort)
                headers["Host"] = Address.Host;
            else
                headers["Host"] = string.Format("{0}:{1}", Address.Host, Address.Port);

            #endregion

            #region Connection и授权

            HttpProxyClient httpProxy = null;

            if (_currentProxy != null && _currentProxy.Type == ProxyType.Http)
            {
                httpProxy = _currentProxy as HttpProxyClient;
            }
            else if (_currentProxy != null && _currentProxy.Type == ProxyType.Chain)
            {
                httpProxy = FindHttpProxyInChain(_currentProxy as ChainProxyClient);
            }

            if (httpProxy != null)
            {
                if (KeepAlive)
                    headers["Proxy-Connection"] = "keep-alive";
                else
                    headers["Proxy-Connection"] = "close";

                if (!string.IsNullOrEmpty(httpProxy.Username) ||
                    !string.IsNullOrEmpty(httpProxy.Password))
                {
                    headers["Proxy-Authorization"] = GetProxyAuthorizationHeader(httpProxy);
                }
            }
            else
            {
                if (KeepAlive)
                    headers["Connection"] = "keep-alive";
                else
                    headers["Connection"] = "close";
            }

            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                headers["Authorization"] = GetAuthorizationHeader();
            }

            #endregion

            #region Content

            if (EnableEncodingContent)
                headers["Accept-Encoding"] = "gzip,deflate";

            if (Culture != null)
                headers["Accept-Language"] = GetLanguageHeader();

            if (CharacterSet != null)
                headers["Accept-Charset"] = GetCharsetHeader();

            if (CanContainsRequestBody(method))
            {
                if (contentLength > 0)
                {
                    headers["Content-Type"] = contentType;
                }

                headers["Content-Length"] = contentLength.ToString();
            }

            #endregion

            return headers;
        }

        #region 标题的工作

        private string GetAuthorizationHeader()
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", Username, Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetProxyAuthorizationHeader(HttpProxyClient httpProxy)
        {
            string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                string.Format("{0}:{1}", httpProxy.Username, httpProxy.Password)));

            return string.Format("Basic {0}", data);
        }

        private string GetLanguageHeader()
        {
            string cultureName;

            if (Culture != null)
                cultureName = Culture.Name;
            else
                cultureName = CultureInfo.CurrentCulture.Name;

            if (cultureName.StartsWith("en"))
                return cultureName;

            return string.Format("{0},{1};q=0.8,en-US;q=0.6,en;q=0.4",
                cultureName, cultureName.Substring(0, 2));
        }

        private string GetCharsetHeader()
        {
            if (CharacterSet == Encoding.UTF8)
            {
                return "utf-8;q=0.7,*;q=0.3";
            }

            string charsetName;

            if (CharacterSet == null)
            {
                charsetName = Encoding.Default.WebName;
            }
            else
            {
                charsetName = CharacterSet.WebName;
            }

            return string.Format("{0},utf-8;q=0.7,*;q=0.3", charsetName);
        }

        private void MergeHeaders(Dictionary<string, string> destination, Dictionary<string, string> source)
        {
            foreach (var sourceItem in source)
            {
                destination[sourceItem.Key] = sourceItem.Value;
            }
        }

        #endregion

        private HttpProxyClient FindHttpProxyInChain(ChainProxyClient chainProxy)
        {
            HttpProxyClient foundProxy = null;

            // 寻找所有HTTP代理代理链。
            // 在找到需要优先代理授权。
            foreach (var proxy in chainProxy.Proxies)
            {
                if (proxy.Type == ProxyType.Http)
                {
                    foundProxy = proxy as HttpProxyClient;

                    if (!string.IsNullOrEmpty(foundProxy.Username) ||
                        !string.IsNullOrEmpty(foundProxy.Password))
                    {
                        return foundProxy;
                    }
                }
                else if (proxy.Type == ProxyType.Chain)
                {
                    HttpProxyClient foundDeepProxy =
                        FindHttpProxyInChain(proxy as ChainProxyClient);

                    if (foundDeepProxy != null &&
                        (!string.IsNullOrEmpty(foundDeepProxy.Username) ||
                        !string.IsNullOrEmpty(foundDeepProxy.Password)))
                    {
                        return foundDeepProxy;
                    }
                }
            }

            return foundProxy;
        }

        private string ToHeadersString(Dictionary<string, string> headers)
        {
            var headersBuilder = new StringBuilder();
            foreach (var header in headers)
            {
                headersBuilder.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }

            headersBuilder.AppendLine();
            return headersBuilder.ToString();
        }

        #endregion

        // 据报道,多少字节发送HTTP服务器。
        private void ReportBytesSent(int bytesSent)
        {
            _bytesSent += bytesSent;

            OnUploadProgressChanged(
                new UploadProgressChangedEventArgs(_bytesSent, _totalBytesSent));
        }

        // 据报道,通过多少字节的HTTP服务器。
        private void ReportBytesReceived(int bytesReceived)
        {
            _bytesReceived += bytesReceived;

            if (_canReportBytesReceived)
            {
                OnDownloadProgressChanged(
                    new DownloadProgressChangedEventArgs(_bytesReceived, _totalBytesReceived));
            }
        }

        // 检查是否可以提出这个话题。
        private bool IsClosedHeader(string name)
        {
            return _closedHeaders.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private void ClearRequestData()
        {
            _content = null;

            _temporaryUrlParams = null;
            _temporaryParams = null;
            _temporaryMultipartContent = null;
            _temporaryHeaders = null;
        }

        private HttpException NewHttpException(string message,
            Exception innerException = null, HttpExceptionStatus status = HttpExceptionStatus.Other)
        {
            return new HttpException(string.Format(message, Address.Host), status, HttpStatusCode.None, innerException);
        }

        #endregion
    }
}