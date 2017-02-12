using System;
using System.Collections.Generic;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 收藏是HTTP cookie。
    /// </summary>
    public class CookieDictionary : Dictionary<string, string>
    {
        /// <summary>
        /// 或返回值,指出是否关闭编辑库克
        /// </summary>
        /// <value>默认值— <see langword="false"/>.</value>
        public bool IsLocked { get; set; }


        /// <summary>
        /// 初始化类的新实例<see cref="CookieDictionary"/>.
        /// </summary>
        /// <param name="isLocked">库克指出,是否关闭编辑。</param>
        public CookieDictionary(bool isLocked = false) : base(StringComparer.OrdinalIgnoreCase)
        {
            IsLocked = isLocked;
        }


        /// <summary>
        /// 返回由字符串名称和cookie值。
        /// </summary>
        /// <returns>行,由名字和cookie值。</returns>
        override public string ToString()
        {
            var strBuilder = new StringBuilder();        

            foreach (var cookie in this)
            {
                strBuilder.AppendFormat("{0}={1}; ", cookie.Key, cookie.Value);
            }

            if (strBuilder.Length > 0)
            {
                strBuilder.Remove(strBuilder.Length - 2, 2);
            }

            return strBuilder.ToString();
        }
    }
}