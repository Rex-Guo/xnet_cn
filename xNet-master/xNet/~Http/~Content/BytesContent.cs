using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// 是身体的请求的字节数。
    /// </summary>
    public class BytesContent : HttpContent
    {
        #region 地板(焊)

        /// <summary>体内容查询。</summary>
        protected byte[] _content;
        /// <summary>体内容字节位移要求。</summary>
        protected int _offset;
        /// <summary>发送字节数的内容。</summary>
        protected int _count;

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">体内容查询。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="content"/> 等于<see langword="null"/>.</exception>
        /// <remarks>使用默认内容类型'application/octet-stream'.</remarks>
        public BytesContent(byte[] content)
            : this(content, 0, content.Length) { }

        /// <summary>
        /// 初始化类的新实例<see cref="BytesContent"/>.
        /// </summary>
        /// <param name="content">体内容查询。</param>
        /// <param name="offset">字节偏移为内容。</param>
        /// <param name="count">发送内容的字节数。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="content"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="offset"/> 少0.
        /// -或-
        /// 参数值<paramref name="offset"/> 更多内容长度。
        /// -或-
        /// 参数值<paramref name="count"/> 少0.
        /// -或-
        /// 参数值<paramref name="count"/> 大(内容长度-位移).</exception>
        /// <remarks>使用默认内容类型'application/octet-stream'.</remarks>
        public BytesContent(byte[] content, int offset, int count)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (offset < 0)
            {
                throw ExceptionHelper.CanNotBeLess("offset", 0);
            }

            if (offset > content.Length)
            {
                throw ExceptionHelper.CanNotBeGreater("offset", content.Length);
            }

            if (count < 0)
            {
                throw ExceptionHelper.CanNotBeLess("count", 0);
            }

            if (count > (content.Length - offset))
            {
                throw ExceptionHelper.CanNotBeGreater("count", content.Length - offset);
            }

            #endregion

            _content = content;
            _offset = offset;
            _count = count;

            _contentType = "application/octet-stream";
        }

        #endregion


        /// <summary>
        /// 初始化类的新实例<see cref="BytesContent"/>.
        /// </summary>
        protected BytesContent() { }


        #region 方法(开放)

        /// <summary>
        /// 计算并返回请求体长度字节。
        /// </summary>
        /// <returns>长度字节请求主体。</returns>
        public override long CalculateContentLength()
        {
            return _content.LongLength;
        }

        /// <summary>
        /// 写入数据流查询物体。
        /// </summary>
        /// <param name="stream">流,将身体哪里记录数据查询。</param>
        public override void WriteTo(Stream stream)
        {
            stream.Write(_content, _offset, _count);
        }

        #endregion
    }
}