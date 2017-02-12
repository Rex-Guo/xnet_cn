using System;
using System.IO;

namespace xNet
{
    /// <summary>
    /// 身体是作为数据流查询的特定文件。
    /// </summary>
    public class FileContent : StreamContent
    {
        /// <summary>
        /// 初始化类的新实例<see cref="FileContent"/> 打开文件流。
        /// </summary>
        /// <param name="pathToContent">文件路径,将内容请求主体。</param>
        /// <param name="bufferSize">字节缓冲区大小为流。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="pathToContent"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="pathToContent"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> 参数值<paramref name="bufferSize"/> 少1.</exception>
        /// <exception cref="System.IO.PathTooLongException">指定路径、文件名或两者最大长度超过系统规定的可能性。例如,基于Windows平台的长度不应超过248 文件名字符,不得超过260 标志。</exception>
        /// <exception cref="System.IO.FileNotFoundException">参数值<paramref name="pathToContent"/> 指向不存在的文件。</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">参数值<paramref name="pathToContent"/> 指向可望通过。</exception>
        /// <exception cref="System.IO.IOException">输入输出错误文件时。</exception>
        /// <exception cref="System.Security.SecurityException">调用语句没有必要的许可。</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// 不支持的文件读写操作当前平台。
        /// -或-
        /// 参数值<paramref name="pathToContent"/> 指定的目录。
        /// -或-
        /// 调用语句没有必要的许可。
        /// </exception>
        /// <remarks>内容类型定义自动根据文件扩展名。</remarks>
        public FileContent(string pathToContent, int bufferSize = 32768)
        {
            #region 检查参数

            if (pathToContent == null)
            {
                throw new ArgumentNullException("pathToContent");
            }

            if (pathToContent.Length == 0)
            {
                throw ExceptionHelper.EmptyString("pathToContent");
            }

            if (bufferSize < 1)
            {
                throw ExceptionHelper.CanNotBeLess("bufferSize", 1);
            }

            #endregion

            _content = new FileStream(pathToContent, FileMode.Open, FileAccess.Read);
            _bufferSize = bufferSize;
            _initialStreamPosition = 0;

            _contentType = Http.DetermineMediaType(
                Path.GetExtension(pathToContent));
        }
    }
}