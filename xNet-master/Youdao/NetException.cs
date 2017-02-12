using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// 例外,在出现错误,抛出与网络。
    /// </summary>
    [Serializable]
    public class NetException : Exception
    {
        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="NetException"/>.
        /// </summary>
        public NetException() : this(Resources.NetException_Default) { }

        /// <summary>
        /// 初始化类的新实例<see cref="NetException"/> 指定错误消息。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        public NetException(string message, Exception innerException = null)
            : base(message, innerException) { }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="NetException"/> 给定实例<see cref="SerializationInfo"/> 和<see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">类的实例<see cref="SerializationInfo"/>, 注意包含所需的新实例序列化类<see cref="NetException"/>.</param>
        /// <param name="streamingContext">类的实例<see cref="StreamingContext"/>, 序列化流源包含有关新类的实例<see cref="NetException"/>.</param>
        protected NetException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext) { }
    }
}