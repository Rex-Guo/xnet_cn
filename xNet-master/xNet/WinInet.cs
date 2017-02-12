using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace xNet
{
    /// <summary>
    /// 类是互动网络配置Windows操作系统。
    /// </summary>
    public static class WinInet
    {
        private const string PathToInternetOptions = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";


        #region 静态特性(开放)

        /// <summary>
        /// 返回值,指出是否安装互联网连接。
        /// </summary>
        public static bool InternetConnected
        {
            get
            {
                SafeNativeMethods.InternetConnectionState state = 0;
                return SafeNativeMethods.InternetGetConnectedState(ref state, 0);
            }
        }

        /// <summary>
        /// 返回值,指出是否安装的互联网模式。
        /// </summary>
        public static bool InternetThroughModem
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_MODEM);
            }
        }

        /// <summary>
        /// 返回值,指出是否安装互联网本地网络。
        /// </summary>
        public static bool InternetThroughLan
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_LAN);
            }
        }

        /// <summary>
        /// 返回值,指出是否安装互联网代理服务器。
        /// </summary>
        public static bool InternetThroughProxy
        {
            get
            {
                return EqualConnectedState(
                    SafeNativeMethods.InternetConnectionState.INTERNET_CONNECTION_PROXY);
            }
        }

        /// <summary>
        /// 返回值指定是否使用代理服务器在Internet Explorer。
        /// </summary>
        public static bool IEProxyEnable
        {
            get
            {
                try
                {
                    return GetIEProxyEnable();
                }
                catch (IOException) { return false; }
                catch (SecurityException) { return false; }
                catch (ObjectDisposedException) { return false; }
                catch (UnauthorizedAccessException) { return false; }
            }
            set
            {
                try
                {
                    SetIEProxyEnable(value);
                }
                catch (IOException) { }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// Возвращает или задаёт прокси-сервер Internet Explorer'a。
        /// </summary>
        /// <value>прокси-серверInternet ExplorerЕсли'而不是问或错误,则返回<see langword="null"/>. 如果给定<see langword="null"/>, прокси-серверInternet Explorerто'而将стёрт。</value>
        public static HttpProxyClient IEProxy
        {
            get
            {
                string proxy;

                try
                {
                    proxy = GetIEProxy();
                }
                catch (IOException) { return null; }
                catch (SecurityException) { return null; }
                catch (ObjectDisposedException) { return null; }
                catch (UnauthorizedAccessException) { return null; }

                HttpProxyClient ieProxy;
                HttpProxyClient.TryParse(proxy, out ieProxy);

                return ieProxy;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        SetIEProxy(value.ToString());
                    }
                    else
                    {
                        SetIEProxy(string.Empty);
                    }
                }
                catch (SecurityException) { }
                catch (ObjectDisposedException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        #endregion


        #region 静态方法(开放)

        /// <summary>
        /// 返回值指定是否使用代理服务器在Internet Explorer。值是从名册。
        /// </summary>
        /// <returns>值,指出是否使用代理服务器在Internet Explorer。</returns>
        /// <exception cref="System.Security.SecurityException">允许用户在没有必要阅读章节名册。</exception>
        /// <exception cref="System.ObjectDisposedException">对象<see cref="Microsoft.Win32.RegistryKey"/>, 调用该方法为关闭(进入封闭不节).</exception>
        /// <exception cref="System.UnauthorizedAccessException">用户需要访问规则缺乏时效性。</exception>
        /// <exception cref="System.IO.IOException">节<see cref="Microsoft.Win32.RegistryKey"/>, 包含了给定值,标记为删除。</exception>
        public static bool GetIEProxyEnable()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                object value = regKey.GetValue("ProxyEnable");

                if (value == null)
                {
                    return false;
                }
                else
                {
                    return ((int)value == 0) ? false : true;
                }
            }
        }

        /// <summary>
        /// 指定值,指出是否使用代理服务器在Internet Explorer。值在指定登记册。
        /// </summary>
        /// <param name="enabled">Указывает, используется ли прокси-сервер в Internet Explorer.</param>
        /// <exception cref="System.Security.SecurityException">没有许可,用户需要创建分区或开放注册。</exception>
        /// <exception cref="System.ObjectDisposedException">对象<see cref="Microsoft.Win32.RegistryKey"/>, 调用该方法为关闭(进入封闭不节).</exception>
        /// <exception cref="System.UnauthorizedAccessException">记录对象<see cref="Microsoft.Win32.RegistryKey"/> 例如,他们可能无法打开,便于记录部分,或用户不需要权限。</exception>
        public static void SetIEProxyEnable(bool enabled)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyEnable", (enabled) ? 1 : 0);
            }
        }

        /// <summary>
        /// Возвращает значение прокси-сервера Internet Explorer'而意义是从注册。。
        /// </summary>
        /// <returns>Значение прокси-сервера Internet Explorer'啊,否则空字符串。</returns>
        /// <exception cref="System.Security.SecurityException">允许用户在没有必要阅读章节名册。</exception>
        /// <exception cref="System.ObjectDisposedException">对象<see cref="Microsoft.Win32.RegistryKey"/>, 调用该方法为关闭(进入封闭不节).</exception>
        /// <exception cref="System.UnauthorizedAccessException">用户需要访问规则缺乏时效性。</exception>
        /// <exception cref="System.IO.IOException">节<see cref="Microsoft.Win32.RegistryKey"/>, 包含了给定值,标记为删除。</exception>
        public static string GetIEProxy()
        {
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(PathToInternetOptions))
            {
                return (regKey.GetValue("ProxyServer") as string) ?? string.Empty;
            }
        }

        /// <summary>
        /// Задаёт значение прокси-сервера Internet Explorer'而登记。值指定。
        /// </summary>
        /// <param name="host">代理服务器主机。</param>
        /// <param name="port">代理服务器的端口。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="host"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="host"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值<paramref name="port"/> 少1 或者更多65535.</exception>
        /// <exception cref="System.Security.SecurityException">没有许可,用户需要创建分区或开放注册。</exception>
        /// <exception cref="System.ObjectDisposedException">对象<see cref="Microsoft.Win32.RegistryKey"/>, 调用该方法为关闭(进入封闭不节).</exception>
        /// <exception cref="System.UnauthorizedAccessException">记录对象<see cref="Microsoft.Win32.RegistryKey"/> 例如,他们可能无法打开,便于记录部分,或用户不需要权限。</exception>
        public static void SetIEProxy(string host, int port)
        {
            #region 检查参数

            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (host.Length == 0)
            {
                throw ExceptionHelper.EmptyString("host");
            }

            if (!ExceptionHelper.ValidateTcpPort(port))
            {
                throw ExceptionHelper.WrongTcpPort("port");
            }

            #endregion

            SetIEProxy(host + ":" + port.ToString());
        }

        /// <summary>
        /// Задаёт значение прокси-сервера Internet Explorer'而登记。值指定。
        /// </summary>
        /// <param name="hostAndPort">代理服务器的主机名和端口的主机格式:港口或只有主机。</param>
        /// <exception cref="System.Security.SecurityException">没有许可,用户需要创建分区或开放注册。</exception>
        /// <exception cref="System.ObjectDisposedException">对象<see cref="Microsoft.Win32.RegistryKey"/>, 调用该方法为关闭(进入封闭不节).</exception>
        /// <exception cref="System.UnauthorizedAccessException">记录对象<see cref="Microsoft.Win32.RegistryKey"/> 例如,他们可能无法打开,便于记录部分,或用户不需要权限。</exception>
        public static void SetIEProxy(string hostAndPort)
        {
            using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PathToInternetOptions))
            {
                regKey.SetValue("ProxyServer", hostAndPort ?? string.Empty);
            }
        }

        #endregion


        private static bool EqualConnectedState(SafeNativeMethods.InternetConnectionState expected)
        {
            SafeNativeMethods.InternetConnectionState state = 0;
            SafeNativeMethods.InternetGetConnectedState(ref state, 0);

            return (state & expected) != 0;
        }
    }
}