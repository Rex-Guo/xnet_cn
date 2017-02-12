using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace xNet
{
    /// <summary>
    /// 提交查询内容состовн体形式。
    /// </summary>
    public class MultipartContent : HttpContent, IEnumerable<HttpContent>
    {
        private sealed class Element
        {
            #region 地板(开放)

            public string Name;
            public string FileName;

            public HttpContent Content;

            #endregion


            public bool IsFieldFile()
            {
                return FileName != null;
            }
        }


        #region 常数(关闭)

        private const int FieldTemplateSize = 43;
        private const int FieldFileTemplateSize = 72;
        private const string FieldTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n";
        private const string FieldFileTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";

        #endregion


        #region 静电场(关闭)

        [ThreadStatic] private static Random _rand;
        private static Random Rand
        {
            get
            {
                if (_rand == null)
                    _rand = new Random();
                return _rand;
            }
        }

        #endregion


        #region 地板(关闭)

        private string _boundary;
        private List<Element> _elements = new List<Element>();

        #endregion


        #region 设计师(开放)

        /// <summary>
        /// 初始化类的新实例<see cref="MultipartContent"/>.
        /// </summary>
        public MultipartContent()
            : this("----------------" + GetRandomString(16)) { }

        /// <summary>
        /// 初始化类的新实例<see cref="MultipartContent"/>.
        /// </summary>
        /// <param name="boundary">国外办事处组成内容。</param>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="boundary"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="boundary"/> 是空字符串。</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">参数值<paramref name="boundary"/> 具有较长70 符号。</exception>
        public MultipartContent(string boundary)
        {
            #region 检查参数

            if (boundary == null)
            {
                throw new ArgumentNullException("boundary");
            }

            if (boundary.Length == 0)
            {
                throw ExceptionHelper.EmptyString("boundary");
            }

            if (boundary.Length > 70)
            {
                throw ExceptionHelper.CanNotBeGreater("boundary", 70);
            }

            #endregion

            _boundary = boundary;

            _contentType = string.Format("multipart/form-data; boundary={0}", _boundary);
        }

        #endregion


        #region 方法(开放)

        /// <summary>
        /// 添加新元素состовн内容体请求。
        /// </summary>
        /// <param name="content">元素值。</param>
        /// <param name="name">元素名称。</param>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        public void Add(HttpContent content, string name)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            #endregion

            var element = new Element()
            {
                Name = name,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// 添加新元素состовн内容体请求。
        /// </summary>
        /// <param name="content">元素值。</param>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">元素的文件名。</param>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        public void Add(HttpContent content, string name, string fileName)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #endregion

            content.ContentType = Http.DetermineMediaType(
                Path.GetExtension(fileName));

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// 添加新元素состовн内容体请求。
        /// </summary>
        /// <param name="content">元素值。</param>
        /// <param name="name">元素名称。</param>
        /// <param name="fileName">元素的文件名。</param>
        /// <param name="contentType">MIME-内容类型。</param>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        /// <exception cref="System.ArgumentNullException">
        /// 参数值<paramref name="content"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="name"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="fileName"/> 等于<see langword="null"/>.
        /// -或-
        /// 参数值<paramref name="contentType"/> 等于<see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="name"/> 是空字符串。</exception>
        public void Add(HttpContent content, string name, string fileName, string contentType)
        {
            #region 检查参数

            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw ExceptionHelper.EmptyString("name");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (contentType == null)
            {
                throw new ArgumentNullException("contentType");
            }

            #endregion

            content.ContentType = contentType;

            var element = new Element()
            {
                Name = name,
                FileName = fileName,
                Content = content
            };

            _elements.Add(element);
        }

        /// <summary>
        /// 计算并返回请求体长度字节。
        /// </summary>
        /// <returns>长度字节请求主体。</returns>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        public override long CalculateContentLength()
        {
            ThrowIfDisposed();

            long length = 0;

            foreach (var element in _elements)
            {
                length += element.Content.CalculateContentLength();

                if (element.IsFieldFile())
                {
                    length += FieldFileTemplateSize;
                    length += element.Name.Length;
                    length += element.FileName.Length;
                    length += element.Content.ContentType.Length;
                }
                else
                {
                    length += FieldTemplateSize;
                    length += element.Name.Length;
                }

                // 2 (--) + x (boundary) + 2 (\r\n) ...数据元素……+ 2 (\r\n).
                length += _boundary.Length + 6;
            }

            // 2 (--) + x (boundary) + 2 (--) + 2 (\r\n).
            length += _boundary.Length + 6;

            return length;
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

            byte[] newLineBytes = Encoding.ASCII.GetBytes("\r\n");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "\r\n");

            foreach (var element in _elements)
            {
                stream.Write(boundaryBytes, 0, boundaryBytes.Length);

                string field;

                if (element.IsFieldFile())
                {
                    field = string.Format(
                        FieldFileTemplate, element.Name, element.FileName, element.Content.ContentType);
                }
                else
                {
                    field = string.Format(
                        FieldTemplate, element.Name);
                }

                byte[] fieldBytes = Encoding.ASCII.GetBytes(field);
                stream.Write(fieldBytes, 0, fieldBytes.Length);

                element.Content.WriteTo(stream);
                stream.Write(newLineBytes, 0, newLineBytes.Length);
            }

            boundaryBytes = Encoding.ASCII.GetBytes("--" + _boundary + "--\r\n");
            stream.Write(boundaryBytes, 0, boundaryBytes.Length);
        }

        /// <summary>
        /// перечеслител返回元素组成内容。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ObjectDisposedException">目前样品已经被删除。</exception>
        public IEnumerator<HttpContent> GetEnumerator()
        {
            ThrowIfDisposed();

            return _elements.Select(e => e.Content).GetEnumerator();
        }

        #endregion


        /// <summary>
        /// 免除失控(并可根据需要控制) 资源使用的对象<see cref="HttpContent"/>.
        /// </summary>
        /// <param name="disposing">意义<see langword="true"/> 允许和不可控释放资源; 意义<see langword="false"/> 只允许自由不羁的资源。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _elements != null)
            {
                foreach (var element in _elements)
                {
                    element.Content.Dispose();
                }

                _elements = null;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();

            return GetEnumerator();
        }


        #region 方法(关闭)

        public static string GetRandomString(int length)
        {
            var strBuilder = new StringBuilder(length);

            for (int i = 0; i < length; ++i)
            {
                switch (Rand.Next(3))
                {
                    case 0:
                        strBuilder.Append((char)Rand.Next(48, 58));
                        break;

                    case 1:
                        strBuilder.Append((char)Rand.Next(97, 123));
                        break;

                    case 2:
                        strBuilder.Append((char)Rand.Next(65, 91));
                        break;
                }
            }

            return strBuilder.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_elements == null)
            {
                throw new ObjectDisposedException("MultipartContent");
            }
        }

        #endregion
    }
}