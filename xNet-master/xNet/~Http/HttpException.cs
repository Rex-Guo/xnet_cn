using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace xNet
{
    /// <summary>
    /// 例外,在出现错误,抛出与HTTP协议。
    /// </summary>
    [Serializable]
    public sealed class HttpException : NetException
    {
        #region 性能(开放)

        /// <summary>
        /// 返回状态异常。
        /// </summary>
        public HttpExceptionStatus Status { get; internal set; }

        /// <summary>
        /// 返回状态码HTTP服务器响应。
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        #endregion


        internal bool EmptyMessageBody { get; set; }


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="HttpException"/>.
        /// </summary>
        public HttpException() : this(Resources.HttpException_Default) { }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpException"/> 指定错误消息。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        public HttpException(string message, Exception innerException = null)
            : base(message, innerException) { }

        /// <summary>
        /// 初始化类的新实例<see cref="HttpException"/> 指定错误状态码和响应。
        /// </summary>
        /// <param name="message">错误消息的原因除外。</param>
        /// <param name="statusCode">响应状态码的HTTP服务器。</param>
        /// <param name="innerException">例外,例外,或引起当前值<see langword="null"/>.</param>
        public HttpException(string message, HttpExceptionStatus status,
            HttpStatusCode httpStatusCode = HttpStatusCode.None, Exception innerException = null)
            : base(message, innerException)
        {
            Status = status;
            HttpStatusCode = httpStatusCode;
        }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="HttpException"/> 给定实例<see cref="SerializationInfo"/> 和<see cref="StreamingContext"/>.
        /// </summary>
        /// <param name="serializationInfo">类的实例<see cref="SerializationInfo"/>, 注意包含所需的新实例序列化类<see cref="HttpException"/>.</param>
        /// <param name="streamingContext">类的实例<see cref="StreamingContext"/>, 序列化流源包含有关新类的实例<see cref="HttpException"/>.</param>
        protected HttpException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
            if (serializationInfo != null)
            {
                Status = (HttpExceptionStatus)serializationInfo.GetInt32("Status");
                HttpStatusCode = (HttpStatusCode)serializationInfo.GetInt32("HttpStatusCode");
            }
        }


        /// <summary>
        /// 填写<see cref="SerializationInfo"/> 所需数据序列化例外<see cref="HttpException"/>.
        /// </summary>
        /// <param name="serializationInfo">序列化数据,<see cref="SerializationInfo"/>, 必须使用。</param>
        /// <param name="streamingContext">序列化数据,<see cref="StreamingContext"/>, 必须使用。</param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            base.GetObjectData(serializationInfo, streamingContext);

            if (serializationInfo != null)
            {
                serializationInfo.AddValue("Status", (int)Status);
                serializationInfo.AddValue("HttpStatusCode", (int)HttpStatusCode);
            }
        }
    }
}