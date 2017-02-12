using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// 客户是用于HTTP代理服务器。
    /// </summary>
    public class HttpProxyClient : ProxyClient
    {
        #region 常数(关闭)

        private const int BufferSize = 50;
        private const int DefaultPort = 8080;

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="HttpProxyClient"/>.
        /// </summary>
        public HttpProxyClient()
            : this(null) { }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpProxyClient"/> 指定代理服务器主机和端口设置为-8080.
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        public HttpProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        public HttpProxyClient(string host, int port)
            : this(host, port, string.Empty, string.Empty) { }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <param name="username">用户授权的代理服务器。</param>
        /// <param name="password">登录代理服务器上。</param>
        public HttpProxyClient(string host, int port, string username, string password)
            : base(ProxyType.Http, host, port, username, password) { }

        #endregion


        #region 静态方法(开放)

        /// <summary>
        /// 转换字符串类实例<see cref="HttpProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <returns>类的实例<see cref="HttpProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public static HttpProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Http, proxyAddress) as HttpProxyClient;
        }

        /// <summary>
        /// 转换字符串类实例<see cref="HttpProxyClient"/>. 返回值,指出是否成功完成转换。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <param name="result">如果顺利完成,包含转换类的实例<see cref="HttpProxyClient"/>, 否则<see langword="null"/>.</param>
        /// <returns>意义<see langword="true"/>, 如果参数<paramref name="proxyAddress"/> 转换成功,否则<see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out HttpProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Http, proxyAddress, out proxy))
            {
                result = proxy as HttpProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        #region 方法(开放)

        /// <summary>
        /// 创建服务器连接代理服务器。
        /// </summary>
        /// <param name="destinationHost">主机服务器,需要通过代理服务器。</param>
        /// <param name="destinationPort">服务器端口,需要通过代理服务器。</param>
        /// <param name="tcpClient">在美国工作的需要,或值<see langword="null"/>.</param>
        /// <returns>与服务器的连接代理。</returns>
        /// <exception cref="System.InvalidOperationException">
        /// 属性值<see cref="Host"/> 等于<see langword="null"/> 或具有零长度。
        /// -或-
        /// 属性值<see cref="Port"/> 少1 或者更多65535.
        /// -或-
        /// 属性值<see cref="Username"/> 具有较长255 符号。
        /// -或-
        /// 属性值<see cref="Password"/> 具有较长255 符号。
        /// </exception>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="destinationHost"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="destinationHost"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值<paramref name="destinationPort"/> 少1 或者更多65535.</exception>
        /// <exception cref="xNet.Net.ProxyException">错误与代理服务器。</exception>
        /// <remarks>如果нерав服务器端口80, 而采用连接'CONNECT'.</remarks>
        public override TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null)
        {
            CheckState();

            #region 检查参数

            if (destinationHost == null)
            {
                throw new ArgumentNullException("destinationHost");
            }

            if (destinationHost.Length == 0)
            {
                throw ExceptionHelper.EmptyString("destinationHost");
            }

            if (!ExceptionHelper.ValidateTcpPort(destinationPort))
            {
                throw ExceptionHelper.WrongTcpPort("destinationPort");
            }

            #endregion

            TcpClient curTcpClient = tcpClient;

            if (curTcpClient == null)
            {
                curTcpClient = CreateConnectionToProxy();
            }

            if (destinationPort != 80)
            {
                HttpStatusCode statusCode = HttpStatusCode.OK;

                try
                {
                    NetworkStream nStream = curTcpClient.GetStream();

                    SendConnectionCommand(nStream, destinationHost, destinationPort);
                    statusCode = ReceiveResponse(nStream);
                }
                catch (Exception ex)
                {
                    curTcpClient.Close();

                    if (ex is IOException || ex is SocketException)
                    {
                        throw NewProxyException(Resources.ProxyException_Error, ex);
                    }

                    throw;
                }

                if (statusCode != HttpStatusCode.OK)
                {
                    curTcpClient.Close();

                    throw new ProxyException(string.Format(
                        Resources.ProxyException_ReceivedWrongStatusCode, statusCode, ToString()), this);
                }
            }

            return curTcpClient;
        }

        #endregion


        #region 方法(关闭)

        private string GenerateAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(_username) || !string.IsNullOrEmpty(_password))
            {
                string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    string.Format("{0}:{1}", _username, _password)));

                return string.Format("Proxy-Authorization: Basic {0}\r\n", data);
            }

            return string.Empty;
        }

        private void SendConnectionCommand(NetworkStream nStream, string destinationHost, int destinationPort)
        {
            var commandBuilder = new StringBuilder();

            commandBuilder.AppendFormat("CONNECT {0}:{1} HTTP/1.1\r\n", destinationHost, destinationPort);
            commandBuilder.AppendFormat(GenerateAuthorizationHeader());
            commandBuilder.AppendLine();

            byte[] buffer = Encoding.ASCII.GetBytes(commandBuilder.ToString());

            nStream.Write(buffer, 0, buffer.Length);
        }

        private HttpStatusCode ReceiveResponse(NetworkStream nStream)
        {
            byte[] buffer = new byte[BufferSize];
            var responseBuilder = new StringBuilder();

            WaitData(nStream);

            do
            {
                int bytesRead = nStream.Read(buffer, 0, BufferSize);
                responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            } while (nStream.DataAvailable);

            string response = responseBuilder.ToString();

            if (response.Length == 0)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedEmptyResponse);
            }

            // 行分配状况。例子: HTTP/1.1 200 OK\r\n
            string strStatus = response.Substring(" ", Http.NewLine);

            int simPos = strStatus.IndexOf(' ');

            if (simPos == -1)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedWrongResponse);
            }

            string statusLine = strStatus.Substring(0, simPos);

            if (statusLine.Length == 0)
            {
                throw NewProxyException(Resources.ProxyException_ReceivedWrongResponse);
            }

            HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(
                typeof(HttpStatusCode), statusLine);

            return statusCode;
        }

        private void WaitData(NetworkStream nStream)
        {
            int sleepTime = 0;
            int delay = (nStream.ReadTimeout < 10) ?
                10 : nStream.ReadTimeout;

            while (!nStream.DataAvailable)
            {
                if (sleepTime >= delay)
                {
                    throw NewProxyException(Resources.ProxyException_WaitDataTimeout);
                }

                sleepTime += 10;
                Thread.Sleep(10);
            }
        }

        #endregion
    }
}