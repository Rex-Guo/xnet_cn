using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 客户代表Socks支持4 代理服务器。
    /// </summary>
    public class Socks4ProxyClient : ProxyClient
    {
        #region 常数(焊)

        internal protected const int DefaultPort = 1080;

        internal protected const byte VersionNumber = 4;
        internal protected const byte CommandConnect = 0x01;
        internal protected const byte CommandBind = 0x02;
        internal protected const byte CommandReplyRequestGranted = 0x5a;
        internal protected const byte CommandReplyRequestRejectedOrFailed = 0x5b;
        internal protected const byte CommandReplyRequestRejectedCannotConnectToIdentd = 0x5c;
        internal protected const byte CommandReplyRequestRejectedDifferentIdentd = 0x5d;

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4ProxyClient"/>.
        /// </summary>
        public Socks4ProxyClient()
            : this(null) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4ProxyClient"/> 指定代理服务器主机和端口设置为-1080.
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        public Socks4ProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4ProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        public Socks4ProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4ProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <param name="username">用户授权的代理服务器。</param>
        public Socks4ProxyClient(string host, int port, string username)
            : base(ProxyType.Socks4, host, port, username, null) { }

        #endregion


        #region 静态方法(关闭)

        /// <summary>
        /// 转换字符串类实例<see cref="Socks4ProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <returns>类的实例<see cref="Socks4ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public static Socks4ProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4, proxyAddress) as Socks4ProxyClient;
        }

        /// <summary>
        /// 转换字符串类实例<see cref="Socks4ProxyClient"/>. 返回值,指出是否成功完成转换。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <param name="result">如果顺利完成,包含转换类的实例<see cref="Socks4ProxyClient"/>, 否则<see langword="null"/>.</param>
        /// <returns>意义<see langword="true"/>, 如果参数<paramref name="proxyAddress"/> 转换成功,否则<see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks4ProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4, proxyAddress, out proxy))
            {
                result = proxy as Socks4ProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


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

            try
            {
                SendCommand(curTcpClient.GetStream(), CommandConnect, destinationHost, destinationPort);
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

            return curTcpClient;
        }


        #region 方法(内焊)

        internal protected virtual void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetIPAddressBytes(destinationHost);
            byte[] dstIp = GetPortBytes(destinationPort);

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            // +----+----+----+----+----+----+----+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+
            //    1    1      2              4           variable       1
            byte[] request = new byte[9 + userId.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstIp.CopyTo(request, 2);
            dstPort.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //   1    1       2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];

            // 如果不满足要求。
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }

        internal protected byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(destinationHost, out ipAddr))
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostAddresses(destinationHost);

                    if (ips.Length > 0)
                    {
                        ipAddr = ips[0];
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is ArgumentException)
                    {
                        throw new ProxyException(string.Format(
                            Resources.ProxyException_FailedGetHostAddresses, destinationHost), this, ex);
                    }

                    throw;
                }
            }

            return ipAddr.GetAddressBytes();
        }

        internal protected byte[] GetPortBytes(int port)
        {
            byte[] array = new byte[2];

            array[0] = (byte)(port / 256);
            array[1] = (byte)(port % 256);

            return array;
        }

        internal protected void HandleCommandError(byte command)
        {
            string errorMessage;

            switch (command)
            {
                case CommandReplyRequestRejectedOrFailed:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedOrFailed;
                    break;

                case CommandReplyRequestRejectedCannotConnectToIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedCannotConnectToIdentd;
                    break;

                case CommandReplyRequestRejectedDifferentIdentd:
                    errorMessage = Resources.Socks4_CommandReplyRequestRejectedDifferentIdentd;
                    break;

                default:
                    errorMessage = Resources.Socks_UnknownError;
                    break;
            }

            string exceptionMsg = string.Format(
                Resources.ProxyException_CommandError, errorMessage, ToString());

            throw new ProxyException(exceptionMsg, this);
        }

        #endregion
    }
}