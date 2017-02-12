using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 客户代表Socks支持5 代理服务器。
    /// </summary>
    public class Socks5ProxyClient : ProxyClient
    {
        #region 常数(关闭)

        private const int DefaultPort = 1080;

        private const byte VersionNumber = 5;
        private const byte Reserved = 0x00;
        private const byte AuthMethodNoAuthenticationRequired = 0x00;
        private const byte AuthMethodGssapi = 0x01;
        private const byte AuthMethodUsernamePassword = 0x02;
        private const byte AuthMethodIanaAssignedRangeBegin = 0x03;
        private const byte AuthMethodIanaAssignedRangeEnd = 0x7f;
        private const byte AuthMethodReservedRangeBegin = 0x80;
        private const byte AuthMethodReservedRangeEnd = 0xfe;
        private const byte AuthMethodReplyNoAcceptableMethods = 0xff;
        private const byte CommandConnect = 0x01;
        private const byte CommandBind = 0x02;
        private const byte CommandUdpAssociate = 0x03;
        private const byte CommandReplySucceeded = 0x00;
        private const byte CommandReplyGeneralSocksServerFailure = 0x01;
        private const byte CommandReplyConnectionNotAllowedByRuleset = 0x02;
        private const byte CommandReplyNetworkUnreachable = 0x03;
        private const byte CommandReplyHostUnreachable = 0x04;
        private const byte CommandReplyConnectionRefused = 0x05;
        private const byte CommandReplyTTLExpired = 0x06;
        private const byte CommandReplyCommandNotSupported = 0x07;
        private const byte CommandReplyAddressTypeNotSupported = 0x08;
        private const byte AddressTypeIPV4 = 0x01;
        private const byte AddressTypeDomainName = 0x03;
        private const byte AddressTypeIPV6 = 0x04;

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="Socks5ProxyClient"/>.
        /// </summary>
        public Socks5ProxyClient()
            : this(null) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks5ProxyClient"/> 指定代理服务器主机和端口设置为-1080.
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        public Socks5ProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks5ProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        public Socks5ProxyClient(string host, int port)
            : this(host, port, string.Empty, string.Empty) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks5ProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <param name="username">用户授权的代理服务器。</param>
        /// <param name="password">登录代理服务器上。</param>
        public Socks5ProxyClient(string host, int port, string username, string password)
            : base(ProxyType.Socks5, host, port, username, password) { }

        #endregion


        #region 静态方法(开放)

        /// <summary>
        /// 转换字符串类实例<see cref="Socks5ProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <returns>类的实例<see cref="Socks5ProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public static Socks5ProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks5, proxyAddress) as Socks5ProxyClient;
        }

        /// <summary>
        /// 转换字符串类实例<see cref="Socks5ProxyClient"/>. 返回值,指出是否成功完成转换。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <param name="result">如果顺利完成,包含转换类的实例<see cref="Socks5ProxyClient"/>, 否则<see langword="null"/>.</param>
        /// <returns>意义<see langword="true"/>, 如果参数<paramref name="proxyAddress"/> 转换成功,否则<see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks5ProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks5, proxyAddress, out proxy))
            {
                result = proxy as Socks5ProxyClient;
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
                NetworkStream nStream = curTcpClient.GetStream();

                InitialNegotiation(nStream);
                SendCommand(nStream, CommandConnect, destinationHost, destinationPort);
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


        #region 方法(关闭)

        private void InitialNegotiation(NetworkStream nStream)
        {
            byte authMethod;

            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                authMethod = AuthMethodUsernamePassword;
            }
            else
            {
                authMethod = AuthMethodNoAuthenticationRequired;
            }

            // +----+----------+----------+
            // |VER | NMETHODS | METHODS  |
            // +----+----------+----------+
            // | 1  |    1     | 1 to 255 |
            // +----+----------+----------+
            byte[] request = new byte[3];

            request[0] = VersionNumber;
            request[1] = 1;
            request[2] = authMethod;

            nStream.Write(request, 0, request.Length);

            // +----+--------+
            // |VER | METHOD |
            // +----+--------+
            // | 1  |   1    |
            // +----+--------+
            byte[] response = new byte[2];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];

            if (authMethod == AuthMethodUsernamePassword && reply == AuthMethodUsernamePassword)
            {
                SendUsernameAndPassword(nStream);
            }
            else if (reply != CommandReplySucceeded)
            {
                HandleCommandError(reply);
            }
        }

        private void SendUsernameAndPassword(NetworkStream nStream)
        {
            byte[] uname = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            byte[] passwd = string.IsNullOrEmpty(_password) ?
                new byte[0] : Encoding.ASCII.GetBytes(_password);

            // +----+------+----------+------+----------+
            // |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
            // +----+------+----------+------+----------+
            // | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
            // +----+------+----------+------+----------+
            byte[] request = new byte[uname.Length + passwd.Length + 3];

            request[0] = 1;
            request[1] = (byte)uname.Length;
            uname.CopyTo(request, 2);
            request[2 + uname.Length] = (byte)passwd.Length;
            passwd.CopyTo(request, 3 + uname.Length);

            nStream.Write(request, 0, request.Length);

            // +----+--------+
            // |VER | STATUS |
            // +----+--------+
            // | 1  |   1    |
            // +----+--------+
            byte[] response = new byte[2];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];

            if (reply != CommandReplySucceeded)
            {
                throw NewProxyException(Resources.ProxyException_Socks5_FailedAuthOn);
            }
        }

        private void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte aTyp = GetAddressType(destinationHost);
            byte[] dstAddr = GetAddressBytes(aTyp, destinationHost);
            byte[] dstPort = GetPortBytes(destinationPort);

            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            byte[] request = new byte[4 + dstAddr.Length + 2];

            request[0] = VersionNumber;
            request[1] = command;
            request[2] = Reserved;
            request[3] = aTyp;
            dstAddr.CopyTo(request, 4);
            dstPort.CopyTo(request, 4 + dstAddr.Length);

            nStream.Write(request, 0, request.Length);

            // +----+-----+-------+------+----------+----------+
            // |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            byte[] response = new byte[255];

            nStream.Read(response, 0, response.Length);

            byte reply = response[1];

            // 如果不满足要求。
            if (reply != CommandReplySucceeded)
            {
                HandleCommandError(reply);
            }
        }

        private byte GetAddressType(string host)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(host, out ipAddr))
            {
                return AddressTypeDomainName;
            }

            switch (ipAddr.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return AddressTypeIPV4;

                case AddressFamily.InterNetworkV6:
                    return AddressTypeIPV6;

                default:
                    throw new ProxyException(string.Format(Resources.ProxyException_NotSupportedAddressType,
                        host, Enum.GetName(typeof(AddressFamily), ipAddr.AddressFamily), ToString()), this);
            }

        }

        private byte[] GetAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case AddressTypeIPV4:
                case AddressTypeIPV6:
                    return IPAddress.Parse(host).GetAddressBytes();

                case AddressTypeDomainName:
                    byte[] bytes = new byte[host.Length + 1];

                    bytes[0] = (byte)host.Length;
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);

                    return bytes;

                default:
                    return null;
            }
        }

        private byte[] GetPortBytes(int port)
        {
            byte[] array = new byte[2];

            array[0] = (byte)(port / 256);
            array[1] = (byte)(port % 256);

            return array;
        }

        private void HandleCommandError(byte command)
        {
            string errorMessage;

            switch (command)
            {
                case AuthMethodReplyNoAcceptableMethods:
                    errorMessage = Resources.Socks5_AuthMethodReplyNoAcceptableMethods;
                    break;

                case CommandReplyGeneralSocksServerFailure:
                    errorMessage = Resources.Socks5_CommandReplyGeneralSocksServerFailure;
                    break;

                case CommandReplyConnectionNotAllowedByRuleset:
                    errorMessage = Resources.Socks5_CommandReplyConnectionNotAllowedByRuleset;
                    break;

                case CommandReplyNetworkUnreachable:
                    errorMessage = Resources.Socks5_CommandReplyNetworkUnreachable;
                    break;

                case CommandReplyHostUnreachable:
                    errorMessage = Resources.Socks5_CommandReplyHostUnreachable;
                    break;

                case CommandReplyConnectionRefused:
                    errorMessage = Resources.Socks5_CommandReplyConnectionRefused;
                    break;

                case CommandReplyTTLExpired:
                    errorMessage = Resources.Socks5_CommandReplyTTLExpired;
                    break;

                case CommandReplyCommandNotSupported:
                    errorMessage = Resources.Socks5_CommandReplyCommandNotSupported;
                    break;

                case CommandReplyAddressTypeNotSupported:
                    errorMessage = Resources.Socks5_CommandReplyAddressTypeNotSupported;
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