using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 不同链构成的代理服务器。
    /// </summary>
    public class ChainProxyClient : ProxyClient
    {
        #region 静电场(关闭)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        #region 地板(关闭)

        private List<ProxyClient> _proxies = new List<ProxyClient>();

        #endregion


        #region 性能(开放)

        /// <summary>
        /// 或返回值,指出是否需要代理服务器列表混合链之前建立新的连接。
        /// </summary>
        public bool EnableShuffle { get; set; }

        /// <summary>
        /// 返回链表的代理服务器。
        /// </summary>
        public List<ProxyClient> Proxies
        {
            get
            {
                return _proxies;
            }
        }

        #region 超定

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public string Host
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public int Port
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public string Username
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public string Password
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public int ConnectTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 此属性不受支持。
        /// </summary>
        /// <exception cref="System.NotSupportedException">任何使用这些属性。</exception>
        override public int ReadWriteTimeout
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="ChainProxyClient"/>.
        /// </summary>
        /// <param name="enableShuffle">指定是否需要代理服务器列表混合链之前建立新的连接。</param>
        public ChainProxyClient(bool enableShuffle = false)
            : base(ProxyType.Chain)
        {
            EnableShuffle = enableShuffle;
        }


        #region 方法(开放)

        /// <summary>
        /// 创建服务器连接链通过代理服务器。
        /// </summary>
        /// <param name="destinationHost">主机服务器,需要通过代理服务器。</param>
        /// <param name="destinationPort">服务器端口,需要通过代理服务器。</param>
        /// <param name="tcpClient">在美国工作的需要,或值<see langword="null"/>.</param>
        /// <returns>与服务器的连接链通过代理服务器。</returns>
        /// <exception cref="System.InvalidOperationException">
        /// 代理服务器的数量相等0.
        /// -或-
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
            #region 检查状态

            if (_proxies.Count == 0)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_ChainProxyClient_NotProxies);
            }

            #endregion

            List<ProxyClient> proxies;

            if (EnableShuffle)
            {
                proxies = _proxies.ToList();

                // 混入代理。
                for (int i = 0; i < proxies.Count; i++)
                {
                    int randI = Rand.Next(proxies.Count);

                    ProxyClient proxy = proxies[i];
                    proxies[i] = proxies[randI];
                    proxies[randI] = proxy;
                }
            }
            else
            {
                proxies = _proxies;
            }

            int length = proxies.Count - 1;
            TcpClient curTcpClient = tcpClient;

            for (int i = 0; i < length; i++)
            {
                curTcpClient = proxies[i].CreateConnection(
                    proxies[i + 1].Host, proxies[i + 1].Port, curTcpClient);
            }

            curTcpClient = proxies[length].CreateConnection(
                destinationHost, destinationPort, curTcpClient);

            return curTcpClient;
        }

        /// <summary>
        /// 主机列表看到新兴行:港口是代理服务器地址。
        /// </summary>
        /// <returns>行看主机列表:港口是代理服务器地址。</returns>
        public override string ToString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToString());
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// 主机列表看到新兴行:港口:名字_用户:密码。最后,添加两个参数设置。
        /// </summary>
        /// <returns>行看主机列表:港口:名字_用户:密码。</returns>
        public virtual string ToExtendedString()
        {
            var strBuilder = new StringBuilder();

            foreach (var proxy in _proxies)
            {
                strBuilder.AppendLine(proxy.ToExtendedString());
            }

            return strBuilder.ToString();
        }

        #region 添加代理服务器

        /// <summary>
        /// 添加新客户代理链。
        /// </summary>
        /// <param name="proxy">添加代理客户。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxy"/> 等于<see langword="null"/>.</exception>
        public void AddProxy(ProxyClient proxy)
        {
            #region 检查参数

            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            #endregion

            _proxies.Add(proxy);
        }

        /// <summary>
        /// 添加新的HTTP代理链中的客户。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public void AddHttpProxy(string proxyAddress)
        {
            _proxies.Add(HttpProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// 添加新的链Socks支持4-代理客户端。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public void AddSocks4Proxy(string proxyAddress)
        {
            _proxies.Add(Socks4ProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// 添加新的链Socks支持4a-代理客户端。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public void AddSocks4aProxy(string proxyAddress)
        {
            _proxies.Add(Socks4aProxyClient.Parse(proxyAddress));
        }

        /// <summary>
        /// 添加新的链Socks支持5-代理客户端。
        /// </summary>
        /// <param name="proxyAddress">行看主机:港口:名字_用户:密码。最后的三参数是可选的。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="proxyAddress"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="proxyAddress"/> 是空字符串。</exception>
        /// <exception cref="System.FormatException">格式不对。港口是</exception>
        public void AddSocks5Proxy(string proxyAddress)
        {
            _proxies.Add(Socks5ProxyClient.Parse(proxyAddress));
        }

        #endregion

        #endregion
    }
}
