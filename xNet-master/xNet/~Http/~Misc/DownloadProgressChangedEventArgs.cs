using System;

namespace xNet
{
    /// <summary>
    /// 数据是对事件进展报告下载数据。
    /// </summary>
    public sealed class DownloadProgressChangedEventArgs : EventArgs
    {
        #region 性能(开放)

        /// <summary>
        /// 收到返回的字节数。
        /// </summary>
        public long BytesReceived { get; private set; }

        /// <summary>
        /// 收到返回的字节总数。
        /// </summary>
        /// <value>如果收到的字节总数未知,则值-1.</value>
        public long TotalBytesToReceive { get; private set; }

        /// <summary>
        /// 返回的字节收到利息。
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                return ((double)BytesReceived / (double)TotalBytesToReceive) * 100.0;
            }
        }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="DownloadProgressChangedEventArgs"/>.
        /// </summary>
        /// <param name="bytesReceived">接收字节数。</param>
        /// <param name="totalBytesToReceive">收到的字节总数。</param>
        public DownloadProgressChangedEventArgs(long bytesReceived, long totalBytesToReceive)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
        }
    }
}