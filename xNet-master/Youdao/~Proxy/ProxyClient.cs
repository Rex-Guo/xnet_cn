using System;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// 实现类的基本构成为代理服务器。
    /// </summary>
    public abstract class ProxyClient : IEquatable<ProxyClient>
    {
        #region 地板(焊)

        /// <summary>代理服务器类型。</summary>
        protected ProxyType _type;

        /// <summary>代理服务器主机。</summary>
        protected string _host;
        /// <summary>代理服务器的端口。</summary>
        protected int _port = 1;
        /// <summary>用户授权的代理服务器。</summary>
        protected string _username;
        /// <summary>登录代理服务器上。</summary>
        protected string _password;

        /// <summary>等待时间为毫秒连接到代理服务器。</summary>
        protected int _connectTimeout = 60000;
        /// <summary>等待时间为毫秒或记录时流读取它。</summary>
        protected int _readWriteTimeout = 60000;

        #endregion


        #region 性能(开放)

        /// <summary>
        /// 代理服务器的返回类型。
        /// </summary>
        public virtual ProxyType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// 返回指定代理服务器或主机。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentNullException">参数值相等<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值为空字符串。</exception>
        public virtual string Host
        {
            get
            {
                return _host;
            }
            set
            {
                #region 检查参数

                if (value == null)
                {
                    throw new ArgumentNullException("Host");
                }

                if (value.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("Host");
                }

                #endregion

                _host = value;
            }
        }

        /// <summary>
        /// 返回指定端口或代理服务器。
        /// </summary>
        /// <value>默认值— 1.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小1 或者更多65535.</exception>
        public virtual int Port
        {
            get
            {
                return _port;
            }
            set
            {
                #region 检查参数

                if (!ExceptionHelper.ValidateTcpPort(value))
                {
                    throw ExceptionHelper.WrongTcpPort("Port");
                }

                #endregion

                _port = value;
            }
        }

        /// <summary>
        /// 返回指定的用户或授权代理服务器。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值具有较长255 符号。</exception>
        public virtual string Username
        {
            get
            {
                return _username;
            }
            set
            {   
                #region 检查参数

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Username", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _username = value;
            }
        }

        /// <summary>
        /// 返回指定的登录或代理服务器。
        /// </summary>
        /// <value>默认值— <see langword="null"/>.</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值具有较长255 符号。</exception>
        public virtual string Password
        {
            get
            {
                return _password;
            }
            set
            {
                #region 检查参数

                if (value != null && value.Length > 255)
                {
                    throw new ArgumentOutOfRangeException("Password", string.Format(
                        Resources.ArgumentOutOfRangeException_StringLengthCanNotBeMore, 255));
                }

                #endregion

                _password = value;
            }
        }

        /// <summary>
        /// 返回指定的毫秒或等待时间连接到代理服务器。
        /// </summary>
        /// <value>默认值60.000, 相当于一分钟。</value>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值较小0.</exception>
        public virtual int ConnectTimeout
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
        public virtual int ReadWriteTimeout
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

        #endregion


        #region 设计师(焊)

        /// <summary>
        /// 初始化类的新实例<see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">代理服务器类型。</param>
        internal protected ProxyClient(ProxyType proxyType)
        {
            _type = proxyType;
        }

        /// <summary>
        /// 初始化类的新实例<see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">代理服务器类型。</param>
        /// <param name="address">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port)
        {
            _type = proxyType;
            _host = address;
            _port = port;
        }

        /// <summary>
        /// 初始化类的新实例<see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">代理服务器类型。</param>
        /// <param name="address">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <param name="username">用户授权的代理服务器。</param>
        /// <param name="password">登录代理服务器上。</param>
        internal protected ProxyClient(ProxyType proxyType, string address, int port, string username, string password)
        {
            _type = proxyType;
            _host = address;
            _port = port;
            _username = username;
            _password = password;
        }

        #endregion


        #region 静态方法(开放)

        /// <summary>
        /// 转换字符串类实例的代理客户,继承了<see cref="ProxyClient"/>.
        /// </summary>
        /// <param name="proxyType">代理服务器类型。</param>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <returns>客户的代理类实例,继承了<see cref="ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        /// <exception cref="System.InvalidOperationException">获得代理服务器类型中。</exception>
        public static ProxyClient Parse(ProxyType proxyType, string proxyAddress)
        {
            #region 检查参数

            if (proxyAddress == null)
            {
                throw new ArgumentNullException("proxyAddress");
            }

            if (proxyAddress.Length == 0)
            {
                throw ExceptionHelper.EmptyString("proxyAddress");
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                #region 接收端口

                try
                {
                    port = int.Parse(values[1]);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw new FormatException(
                            Resources.InvalidOperationException_ProxyClient_WrongPort, ex);
                    }

                    throw;
                }

                if (!ExceptionHelper.ValidateTcpPort(port))
                {
                    throw new FormatException(
                        Resources.InvalidOperationException_ProxyClient_WrongPort);
                }

                #endregion
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            return ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
        }

        /// <summary>
        /// 转换字符串类实例的代理客户,继承了<see cref="ProxyClient"/>. 返回值,指出是否成功完成转换。
        /// </summary>
        /// <param name="proxyType">代理服务器类型。</param>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <param name="result">如果执行成功,则转换代理类实例包含了客户<see cref="ProxyClient"/>, 否则<see langword="null"/>.</param>
        /// <returns>意义<see langword="true"/>, 如果参数<paramref name="proxyAddress"/> 转换成功,否则<see langword="false"/>.</returns>
        public static bool TryParse(ProxyType proxyType, string proxyAddress, out ProxyClient result)
        {
            result = null;

            #region 检查参数

            if (string.IsNullOrEmpty(proxyAddress))
            {
                return false;
            }

            #endregion

            string[] values = proxyAddress.Split(':');

            int port = 0;
            string host = values[0];

            if (values.Length >= 2)
            {
                if (!int.TryParse(values[1], out port) || !ExceptionHelper.ValidateTcpPort(port))
                {
                    return false;
                }
            }

            string username = null;
            string password = null;

            if (values.Length >= 3)
            {
                username = values[2];
            }

            if (values.Length >= 4)
            {
                password = values[3];
            }

            try
            {
                result = ProxyHelper.CreateProxyClient(proxyType, host, port, username, password);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return true;
        }

        #endregion


        /// <summary>
        /// 创建服务器连接代理服务器。
        /// </summary>
        /// <param name="destinationHost">目的地主机,需要通过代理服务器。</param>
        /// <param name="destinationPort">目的地港口,需要通过代理服务器。</param>
        /// <param name="tcpClient">在美国工作的需要,或值<see langword="null"/>.</param>
        /// <returns>代理服务器连接。</returns>
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
        public abstract TcpClient CreateConnection(string destinationHost, int destinationPort, TcpClient tcpClient = null);


        #region 方法(开放)

        /// <summary>
        /// 主机看到新兴行:港口是代理服务器地址。
        /// </summary>
        /// <returns>行看主机:港口是代理服务器地址。</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", _host, _port);
        }

        /// <summary>
        /// 主机看到新兴行:港口:名字_用户:密码。最后,添加两个参数设置。
        /// </summary>
        /// <returns>行看主机:港口:名字_用户:密码。</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendFormat("{0}:{1}", _host, _port);

            if (!string.IsNullOrEmpty(_username))
            {
                strBuilder.AppendFormat(":{0}", _username);

                if (!string.IsNullOrEmpty(_password))
                {
                    strBuilder.AppendFormat(":{0}", _password);
                }
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// hash码返回代理客户。
        /// </summary>
        /// <returns>作为hash码32-位带符号整数。</returns>
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(_host))
            {
                return 0;
            }

            return (_host.GetHashCode() ^ _port);
        }

        /// <summary>
        /// 确定是否等于两个代理客户。
        /// </summary>
        /// <param name="proxy">为客户代理实例数据对比。</param>
        /// <returns>意义<see langword="true"/>, 如果两个代理客户同等重要,否则<see langword="false"/>.</returns>
        public bool Equals(ProxyClient proxy)
        {
            if (proxy == null || _host == null)
            {
                return false;
            }

            if (_host.Equals(proxy._host,
                StringComparison.OrdinalIgnoreCase) && _port == proxy._port)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 确定是否等于两个代理客户。
        /// </summary>
        /// <param name="obj">为客户代理实例数据对比。</param>
        /// <returns>意义<see langword="true"/>, 如果两个代理客户同等重要,否则<see langword="false"/>.</returns>
        public override bool Equals(object obj)
        {
            var proxy = obj as ProxyClient;

            if (proxy == null)
            {
                return false;
            }

            return Equals(proxy);
        }

        #endregion


        #region 方法(焊)

        /// <summary>
        /// 创建连接代理服务器。
        /// </summary>
        /// <returns>代理服务器连接。</returns>
        /// <exception cref="xNet.Net.ProxyException">错误与代理服务器。</exception>
        protected TcpClient CreateConnectionToProxy()
        {
            TcpClient tcpClient = null;

            #region 建立连接

            tcpClient = new TcpClient();
            Exception connectException = null;
            ManualResetEventSlim connectDoneEvent = new ManualResetEventSlim();

            try
            {
                tcpClient.BeginConnect(_host, _port, new AsyncCallback(
                    (ar) =>
                    {
                        if (tcpClient.Client != null)
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
                        }
                    }), tcpClient
                );
            }
            #region Catch's

            catch (Exception ex)
            {
                tcpClient.Close();

                if (ex is SocketException || ex is SecurityException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, ex);
                }

                throw;
            }

            #endregion

            if (!connectDoneEvent.Wait(_connectTimeout))
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_ConnectTimeout);
            }

            if (connectException != null)
            {
                tcpClient.Close();

                if (connectException is SocketException)
                {
                    throw NewProxyException(Resources.ProxyException_FailedConnect, connectException);
                }
                else
                {
                    throw connectException;
                }
            }

            if (!tcpClient.Connected)
            {
                tcpClient.Close();
                throw NewProxyException(Resources.ProxyException_FailedConnect);
            }

            #endregion

            tcpClient.SendTimeout = _readWriteTimeout;
            tcpClient.ReceiveTimeout = _readWriteTimeout;

            return tcpClient;
        }

        /// <summary>
        /// 检查代理客户不同参数值错误。
        /// </summary>
        /// <exception cref="System.InvalidOperationException">属性值<see cref="Host"/> 等于<see langword="null"/> 或具有零长度。</exception>
        /// <exception cref="System.InvalidOperationException">属性值<see cref="Port"/> 少1 或者更多65535.</exception>
        /// <exception cref="System.InvalidOperationException">属性值<see cref="Username"/> 具有较长255 符号。</exception>
        /// <exception cref="System.InvalidOperationException">属性值<see cref="Password"/> 具有较长255 符号。</exception>
        protected void CheckState()
        {
            if (string.IsNullOrEmpty(_host))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongHost);
            }

            if (!ExceptionHelper.ValidateTcpPort(_port))
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPort);
            }

            if (_username != null && _username.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongUsername);
            }

            if (_password != null && _password.Length > 255)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ProxyClient_WrongPassword);
            }
        }

        /// <summary>
        /// 创建代理对象例外。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        /// <returns>除代理对象。</returns>
        protected ProxyException NewProxyException(
            string message, Exception innerException = null)
        {
            return new ProxyException(string.Format(
                message, ToString()), this, innerException);
        }

        #endregion
    }
}