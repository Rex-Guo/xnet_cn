using System.IO;

namespace xNet
{
    /// <summary>
    /// 提交请求主体。освбожда后马上发货。
    /// </summary>
    public abstract class HttpContent
    {
        /// <summary>MIME-内容类型。</summary>
        protected string _contentType = string.Empty;


        /// <summary>
        /// 返回指定MIME类型或内容。
        /// </summary>
        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value ?? string.Empty;
            }
        }


        #region 方法(开放)

        /// <summary>
        /// 计算并返回请求体长度字节。
        /// </summary>
        /// <returns>长度字节请求主体。</returns>
        public abstract long CalculateContentLength();

        /// <summary>
        /// 写入数据流查询物体。
        /// </summary>
        /// <param name="stream">流,将身体哪里记录数据查询。</param>
        public abstract void WriteTo(Stream stream);

        /// <summary>
        /// 释放所有资源,使用当前类的实例<see cref="HttpContent"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion


        /// <summary>
        /// 免除失控(并可根据需要控制) 资源使用的对象<see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">意义<see langword="true"/> 允许和不可控释放资源; 意义<see langword="false"/> 只允许自由不羁的资源。</param>
        protected virtual void Dispose(bool disposing) { }
    }
}