using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// 是身体的请求流。
    /// </summary>
    public class StreamContent : HttpContent
    {
        #region 地板(电磁辐射防护)

        /// <summary>体内容查询。</summary>
        protected Stream _content;
        /// <summary>字节缓冲区大小为流。</summary>
        protected int _bufferSize;
        /// <summary>字节位置,开始读取的数据流。</summary>
        protected long _initialStreamPosition;

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="StreamContent"/>.
        /// </summary>
        /// <param name="content">体内容查询。</param>
        /// <param name="bufferSize">字节缓冲区大小为流。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="content"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">流<paramref name="content"/> 不支持读或移动位置。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> 参数值<paramref name="bufferSize"/> 少1.</exception>
        /// <remarks>使用默认内容类型'application/octet-stream'.</remarks>
        public StreamContent(Stream content, int bufferSize = 32768)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (!content.CanRead || !content.CanSeek)
            {
                throw new ArgumentException(Resources.ArgumentException_CanNotReadOrSeek, "content");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = content;
            _bufferSize = bufferSize;
            _initialStreamPosition = _content.Position;

            _contentType = "application/octet-stream";
        }


        /// <summary>
        /// 初始化类的新实例<see cref="StreamContent"/>.
        /// </summary>
        protected StreamContent() { }


        #region 方法(开放)

        /// <summary>
        /// 计算并返回请求体长度字节。
        /// </summary>
        /// <returns>内容长度字节。</returns>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            return _content.Length;
        }

        /// <summary>
        /// 写入数据流查询物体。
        /// </summary>
        /// <param name="stream">流,将身体哪里记录数据查询。</param>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="stream"/> 等于<see langword="null"/>.</exception>
        public override void WriteTo(Stream stream)
        {
            ThrowIfDisposed();

            #region 检查参数

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            #endregion

            _content.Position = _initialStreamPosition;

            var buffer = new byte[_bufferSize];

            while (true)
            {
                int bytesRead = _content.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                stream.Write(buffer, 0, bytesRead);
            }
        }

        #endregion


        /// <summary>
        /// 免除失控(并可根据需要控制) 资源使用的对象<see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">意义<see langword="true"/> 允许和不可控释放资源; 意义<see langword="false"/> 只允许自由不羁的资源。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _content != null)
            {
                _content.Dispose();
                _content = null;
            }
        }


        private void ThrowIfDisposed()
        {
            if (_content == null)
            {
                throw new ObjectDisposedException("StreamContent");
            }
        }
    }
}