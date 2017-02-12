using System;

namespace xNet
{
    /// <summary>
    /// 数据是对事件进展报告数据卸载。
    /// </summary>
    public sealed class UploadProgressChangedEventArgs : EventArgs
    {
        #region 性能(开放)

        /// <summary>
        /// 返回发送字节数。
        /// </summary>
        public long BytesSent { get; private set; }

        /// <summary>
        /// 返回发送的字节总数。
        /// </summary>
        public long TotalBytesToSend { get; private set; }

        /// <summary>
        /// 返回的字节发送率。
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                return ((double)BytesSent / (double)TotalBytesToSend) * 100.0;
            }
        }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="UploadProgressChangedEventArgs"/>.
        /// </summary>
        /// <param name="bytesSent">发送的字节数。</param>
        /// <param name="totalBytesToSend">发送的字节总数。</param>
        public UploadProgressChangedEventArgs(long bytesSent, long totalBytesToSend)
        {
            BytesSent = bytesSent;
            TotalBytesToSend = totalBytesToSend;
        }
    }
}