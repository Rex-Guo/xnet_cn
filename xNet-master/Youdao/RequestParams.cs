using System;
using System.Collections.Generic;

namespace xNet
{
    /// <summary>
    /// 行是构成查询参数集合。
    /// </summary>
    public class RequestParams : List<KeyValuePair<string,string>>
    {
        /// <summary>
        /// 问新查询参数。
        /// </summary>
        /// <param name="paramName">请求参数名称。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="paramName"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="paramName"/> 是空字符串。</exception>
        public object this[string paramName]
        {
            set
            {
                #region 检查参数

                if (paramName == null)
                {
                    throw new ArgumentNullException("paramName");
                }

                if (paramName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("paramName");
                }

                #endregion

                string str = (value == null ? string.Empty : value.ToString());

                Add(new KeyValuePair<string, string>(paramName, str));
            }
        }
    }
}