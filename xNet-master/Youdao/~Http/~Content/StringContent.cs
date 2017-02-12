using System;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 是身体的请求字符串。
    /// </summary>
    public class StringContent : BytesContent
    {
        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="StringContent"/>.
        /// </summary>
        /// <param name="content">内容内容。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="content"/> 等于<see langword="null"/>.</exception>
        /// <remarks>使用默认内容类型'text/plain'.</remarks>
        public StringContent(string content)
            : this(content, Encoding.UTF8) { }

        /// <summary>
        /// 初始化类的新实例<see cref="StringContent"/>.
        /// </summary>
        /// <param name="content">内容内容。</param>
        /// <param name="encoding">采用编码转换为字节序列数据。</param>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="encoding"/> 等于<see langword="null"/>.
        /// </exception>
        /// <remarks>使用默认内容类型'text/plain'.</remarks>
        public StringContent(string content, Encoding encoding)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            #endregion

            _content = encoding.GetBytes(content);
            _offset = 0;
            _count = _content.Length;

            _contentType = "text/plain";
        }

        #endregion
    }
}