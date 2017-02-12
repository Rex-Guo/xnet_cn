using System;
using System.Collections.Generic;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 身体是作为查询参数查询。
    /// </summary>
    public class FormUrlEncodedContent : BytesContent
    {
        /// <summary>
        /// 初始化类的新实例<see cref="FormUrlEncodedContent"/>.
        /// </summary>
        /// <param name="content">体内容查询的查询参数。</param>
        /// <param name="dontEscape">是否需要指定编码的参数值。</param>
        /// <param name="encoding">编码转换,用于查询参数。如果参数值相等<see langword="null"/>, 则使用值<see cref="System.Text.Encoding.UTF8"/>.</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="content"/> 等于<see langword="null"/>.</exception>
        /// <remarks>使用默认内容类型'application/x-www-form-urlencoded'.</remarks>
        public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> content, bool dontEscape = false, Encoding encoding = null)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            #endregion

            string queryString = Http.ToPostQueryString(content, dontEscape, encoding);

            _content = Encoding.ASCII.GetBytes(queryString);
            _offset = 0;
            _count = _content.Length;

            _contentType = "application/x-www-form-urlencoded";
        }
    }
}