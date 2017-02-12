using System.Net.Sockets;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 客户代表Socks支持4a 代理服务器。
    /// </summary>
    public class Socks4aProxyClient : Socks4ProxyClient 
    {
        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4aProxyClient"/>.
        /// </summary>
        public Socks4aProxyClient()
            : this(null) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4aProxyClient"/> 指定代理服务器主机和端口设置为-1080.
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        public Socks4aProxyClient(string host)
            : this(host, DefaultPort) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4aProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        public Socks4aProxyClient(string host, int port)
            : this(host, port, string.Empty) { }

        /// <summary>
        /// 初始化类的新实例<see cref="Socks4aProxyClient"/> 指定代理服务器数据。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <param name="username">用户授权的代理服务器。</param>
        public Socks4aProxyClient(string host, int port, string username)
            : base(host, port, username)
        {
            _type = ProxyType.Socks4a;
        }

        #endregion


        #region 方法(开放)

        /// <summary>
        /// 转换字符串类实例<see cref="Socks4aProxyClient"/>.
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <returns>类的实例<see cref="Socks4aProxyClient"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public static Socks4aProxyClient Parse(string proxyAddress)
        {
            return ProxyClient.Parse(ProxyType.Socks4a, proxyAddress) as Socks4aProxyClient;
        }

        /// <summary>
        /// 转换字符串类实例<see cref="Socks4aProxyClient"/>. 返回值,指出是否成功完成转换。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <param name="result">如果顺利完成,包含转换类的实例<see cref="Socks4aProxyClient"/>, 否则<see langword="null"/>.</param>
        /// <returns>意义<see langword="true"/>, 如果参数<paramref name="proxyAddress"/> 转换成功,否则<see langword="false"/>.</returns>
        public static bool TryParse(string proxyAddress, out Socks4aProxyClient result)
        {
            ProxyClient proxy;

            if (ProxyClient.TryParse(ProxyType.Socks4a, proxyAddress, out proxy))
            {
                result = proxy as Socks4aProxyClient;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        #endregion


        internal protected override void SendCommand(NetworkStream nStream, byte command, string destinationHost, int destinationPort)
        {
            byte[] dstPort = GetPortBytes(destinationPort);
            byte[] dstIp = { 0, 0, 0, 1 };

            byte[] userId = string.IsNullOrEmpty(_username) ?
                new byte[0] : Encoding.ASCII.GetBytes(_username);

            byte[] dstAddr = ASCIIEncoding.ASCII.GetBytes(destinationHost);

            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            // | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL| DSTADDR      |NULL|
            // +----+----+----+----+----+----+----+----+----+----+....+----+----+----+....+----+
            //    1    1      2              4           variable       1    variable        1 
            byte[] request = new byte[10 + userId.Length + dstAddr.Length];

            request[0] = VersionNumber;
            request[1] = command;
            dstPort.CopyTo(request, 2);
            dstIp.CopyTo(request, 4);
            userId.CopyTo(request, 8);
            request[8 + userId.Length] = 0x00;
            dstAddr.CopyTo(request, 9 + userId.Length);
            request[9 + userId.Length + dstAddr.Length] = 0x00;

            nStream.Write(request, 0, request.Length);

            // +----+----+----+----+----+----+----+----+
            // | VN | CD | DSTPORT |      DSTIP        |
            // +----+----+----+----+----+----+----+----+
            //    1    1      2              4
            byte[] response = new byte[8];

            nStream.Read(response, 0, 8);

            byte reply = response[1];

            // 如果不满足要求。
            if (reply != CommandReplyRequestGranted)
            {
                HandleCommandError(reply);
            }
        }
    }
}