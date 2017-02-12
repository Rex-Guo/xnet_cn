using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// 例外,在出现错误,抛出与代理。
    /// </summary>
    [Serializable]
    public sealed class ProxyException : NetException
    {
        /// <summary>
        /// 代理客户返回的错误。
        /// </summary>
        public ProxyClient ProxyClient { get; private set; }

        
        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="ProxyException"/>.
        /// </summary>
        public ProxyException() : this(Resources.ProxyException_Default) { }

        /// <summary>
        /// 初始化类的新实例<see cref="ProxyException"/> 指定错误消息。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        public ProxyException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// 初始化类的新实例<see cref="xNet.Net.ProxyException"/> 指定错误信息和代理客户。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="proxyClient">代理客户的错误。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        public ProxyException(string message, ProxyClient proxyClient, Exception innerException = null)
            : base(message, innerException)
        {
            ProxyClient = proxyClient;
        }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="ProxyException"/> 给定实例<see cref="SerializationInfo"/> 和<see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">类的实例<see cref="SerializationInfo"/>, 注意包含所需的新实例序列化类<see cref="ProxyException"/>.</param>
        /// <param name="streamingContext">类的实例<see cref="StreamingContext"/>, 序列化流源包含有关新类的实例<see cref="ProxyException"/>.</param>
        protected ProxyException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}