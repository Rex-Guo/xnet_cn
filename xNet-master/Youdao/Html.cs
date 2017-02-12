using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace xNet
{
    /// <summary>
    /// 静态类是用来援助其他HTML和文本数据。
    /// </summary>
    public static class Html
    {
        #region 静电场(关闭)

        private static readonly Dictionary<string, string> _htmlMnemonics = new Dictionary<string, string>()
        {
            { "apos", "'" },
            { "quot", "\"" },
            { "amp", "&" },
            { "lt", "<" },
            { "gt", ">" }
        };

        #endregion


        #region 静态方法(开放)

        /// <summary>
        /// 替换字符串的HTML实体代表符号。
        /// </summary>
        /// <param name="str">将字符串进行替换。</param>
        /// <returns>与HTML字符串取代实体。</returns>
        /// <remarks>只替换下记忆: apos, quot, amp, lt 和gt。以及各种代码。</remarks>
        public static string ReplaceEntities(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"(\&(?<text>\w{1,4})\;)|(\&#(?<code>\w{1,4})\;)", RegexOptions.Compiled);

            string result = regex.Replace(str, match =>
            {
                if (match.Groups["text"].Success)
                {
                    string value;

                    if (_htmlMnemonics.TryGetValue(match.Groups["text"].Value, out value))
                    {
                        return value;
                    }
                }
                else if (match.Groups["code"].Success)
                {
                    int code = int.Parse(match.Groups["code"].Value);
                    return ((char)code).ToString();
                }

                return match.Value;
            });

            return result;
        }

        /// <summary>
        /// 替换字符串中的字符实体代表他们。
        /// </summary>
        /// <param name="str">将字符串进行替换。</param>
        /// <returns>行实体取代。</returns>
        /// <remarks>Unicode-有看到本质: \u2320 或\U044F</remarks>
        public static string ReplaceUnicode(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            var regex = new Regex(@"\\u(?<code>[0-9a-f]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            string result = regex.Replace(str, match =>
            {
                int code = int.Parse(match.Groups["code"].Value, NumberStyles.HexNumber);

                return ((char)code).ToString();
            });

            return result;
        }

        #region 行工作

        /// <summary>
        /// 提取字符串的字符串。字符串开始位置字符串结束<paramref name="left"/> 到行尾。搜索从指定位置。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="startIndex">立场,开始查找字符串。从读0.</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="startIndex"/> 少0.
        /// -或-
        /// 参数值<paramref name="startIndex"/> 等于或大于字符串长度<paramref name="str"/>.
        /// </exception>
        public static string Substring(this string str, string left,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region 检查参数

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // 找字符串开始位置左边。
            int leftPosBegin = str.IndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // 计算字符串左端位置。
            int leftPosEnd = leftPosBegin + left.Length;

            // 计算字符串长度所得。
            int length = str.Length - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// 提取字符串的字符串。字符串开始位置字符串结束<paramref name="left"/> 到行尾。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 是空字符串。</exception>
        public static string Substring(this string str,
            string left, StringComparison comparsion = StringComparison.Ordinal)
        {
            return Substring(str, left, 0, comparsion);
        }

        /// <summary>
        /// 提取字符串的字符串。给定字符串中寻找两者之间从预定位置。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="startIndex">立场,开始查找字符串。从读0.</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="startIndex"/> 少0.
        /// -或-
        /// 参数值<paramref name="startIndex"/> 等于或大于字符串长度<paramref name="str"/>.
        /// </exception>
        public static string Substring(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region 检查参数

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // 找字符串开始位置左边。
            int leftPosBegin = str.IndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // 计算字符串左端位置。
            int leftPosEnd = leftPosBegin + left.Length;

            // 找字符串开始位置规则。
            int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

            if (rightPos == -1)
            {
                return string.Empty;
            }

            // 计算字符串长度所得。
            int length = rightPos - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// 提取字符串的字符串。两行之间寻找字符串指定。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        public static string Substring(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            return str.Substring(left, right, 0, comparsion);
        }

        /// <summary>
        /// 提取字符串中的最后一行。字符串开始位置字符串结束<paramref name="left"/> 到行尾。搜索从指定位置。
        /// </summary>
        /// <param name="str">将字符串中搜索过去。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="startIndex">立场,开始查找字符串。从读0.</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="startIndex"/> 少0.
        /// -或-
        /// 参数值<paramref name="startIndex"/> 等于或大于字符串长度<paramref name="str"/>.
        /// </exception>
        public static string LastSubstring(this string str, string left,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region 检查参数

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // 找字符串开始位置左边。
            int leftPosBegin = str.LastIndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // 计算字符串左端位置。
            int leftPosEnd = leftPosBegin + left.Length;

            // 计算字符串长度所得。
            int length = str.Length - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// 提取字符串中的最后一行。字符串开始位置字符串结束<paramref name="left"/> 到行尾。
        /// </summary>
        /// <param name="str">将字符串中搜索过去。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 是空字符串。</exception>
        public static string LastSubstring(this string str,
            string left, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return LastSubstring(str, left, str.Length - 1, comparsion);
        }

        /// <summary>
        /// 提取字符串中的最后一行。给定字符串中寻找两者之间从预定位置。
        /// </summary>
        /// <param name="str">将字符串中搜索过去。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="startIndex">立场,开始查找字符串。从读0.</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="startIndex"/> 少0.
        /// -或-
        /// 参数值<paramref name="startIndex"/> 等于或大于字符串长度<paramref name="str"/>.
        /// </exception>
        public static string LastSubstring(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            #region 检查参数

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            // 找字符串开始位置左边。
            int leftPosBegin = str.LastIndexOf(left, startIndex, comparsion);

            if (leftPosBegin == -1)
            {
                return string.Empty;
            }

            // 计算字符串左端位置。
            int leftPosEnd = leftPosBegin + left.Length;

            // 找字符串开始位置规则。
            int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

            if (rightPos == -1)
            {
                if (leftPosBegin == 0)
                {
                    return string.Empty;
                }
                else
                {
                    return LastSubstring(str, left, right, leftPosBegin - 1, comparsion);
                }
            }

            // 计算字符串长度所得。
            int length = rightPos - leftPosEnd;

            return str.Substring(leftPosEnd, length);
        }

        /// <summary>
        /// 提取字符串中的最后一行。两行之间寻找字符串指定。
        /// </summary>
        /// <param name="str">将字符串中搜索过去。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>已知字符串或空字符串。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        public static string LastSubstring(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            return str.LastSubstring(left, right, str.Length - 1, comparsion);
        }

        /// <summary>
        /// 提取字符串的字符串。给定字符串中寻找两者之间从预定位置。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="startIndex">立场,开始查找字符串。从读0.</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>发现空字符串或字符串数组。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// 参数值<paramref name="startIndex"/> 少0.
        /// -或-
        /// 参数值<paramref name="startIndex"/> 等于或大于字符串长度<paramref name="str"/>.
        /// </exception>
        public static string[] Substrings(this string str, string left, string right,
            int startIndex, StringComparison comparsion = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new string[0];
            }

            #region 检查参数

            if (left == null)
            {
                throw new ArgumentNullException("left");
            }

            if (left.Length == 0)
            {
                throw ExceptionHelper.EmptyString("left");
            }

            if (right == null)
            {
                throw new ArgumentNullException("right");
            }

            if (right.Length == 0)
            {
                throw ExceptionHelper.EmptyString("right");
            }

            if (startIndex < 0)
            {
                throw ExceptionHelper.CanNotBeLess("startIndex", 0);
            }

            if (startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex",
                    Resources.ArgumentOutOfRangeException_StringHelper_MoreLengthString);
            }

            #endregion

            int currentStartIndex = startIndex;
            List<string> strings = new List<string>();

            while (true)
            {
                // 找字符串开始位置左边。
                int leftPosBegin = str.IndexOf(left, currentStartIndex, comparsion);

                if (leftPosBegin == -1)
                {
                    break;
                }

                // 计算字符串左端位置。
                int leftPosEnd = leftPosBegin + left.Length;

                // 开始寻找位置法行。
                int rightPos = str.IndexOf(right, leftPosEnd, comparsion);

                if (rightPos == -1)
                {
                    break;
                }

                // 计算字符串长度所得。
                int length = rightPos - leftPosEnd;

                strings.Add(str.Substring(leftPosEnd, length));

                // 计算规则字符串结束位置。
                currentStartIndex = rightPos + right.Length;
            }

            return strings.ToArray();
        }

        /// <summary>
        /// 提取字符串的字符串。两行之间寻找字符串指定。
        /// </summary>
        /// <param name="str">将字符串中查找字符串。</param>
        /// <param name="left">行,左侧显示未知字符串。</param>
        /// <param name="right">行,位于右边的未知字符串。</param>
        /// <param name="comparsion">枚举值之一,定义规则搜索。</param>
        /// <returns>发现空字符串或字符串数组。</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="left"/> 或<paramref name="right"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="left"/> 或<paramref name="right"/> 是空字符串。</exception>
        public static string[] Substrings(this string str, string left, string right,
            StringComparison comparsion = StringComparison.Ordinal)
        {
            return str.Substrings(left, right, 0, comparsion);
        }

        #endregion

        #endregion
    }
}
