using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace xNet
{
    /// <summary>
    /// 是предназначенн类下载HTTP服务器的响应。
    /// </summary>
    public sealed class HttpResponse
    {
        #region 年级(关闭)

        // 数组封套字节。
        // 指定的字节数组中的真实。
        private sealed class BytesWraper
        {
            public int Length { get; set; }

            public byte[] Value { get; set; }
        }

        // 这个类用来下载原始数据。
        // 但是他们也使用和下载消息体,没错,他只是卸下余额数据,获取下载原始数据。
        private sealed class ReceiverHelper
        {
            private const int InitialLineSize = 1000;


            #region 地板(关闭)

            private Stream _stream;

            private byte[] _buffer;
            private int _bufferSize;

            private int _linePosition;
            private byte[] _lineBuffer = new byte[InitialLineSize];

            #endregion


            #region 性能(开放)

            public bool HasData
            {
                get
                {
                    return (Length - Position) != 0;
                }
            }

            public int Length { get; private set; }

            public int Position { get; private set; }

            #endregion


            public ReceiverHelper(int bufferSize)
            {
                _bufferSize = bufferSize;
                _buffer = new byte[_bufferSize];
            }


            #region 方法(开放)

            public void Init(Stream stream)
            {
                _stream = stream;
                _linePosition = 0;

                Length = 0;
                Position = 0;
            }

            public string ReadLine()
            {
                _linePosition = 0;

                while (true)
                {
                    if (Position == Length)
                    {
                        Position = 0;
                        Length = _stream.Read(_buffer, 0, _bufferSize);

                        if (Length == 0)
                        {
                            break;
                        }
                    }

                    byte b = _buffer[Position++];

                    _lineBuffer[_linePosition++] = b;

                    // 如果认为符号'\n'.
                    if (b == 10)
                    {
                        break;
                    }

                    // 如果达到最大极限尺寸线缓冲区。
                    if (_linePosition == _lineBuffer.Length)
                    {
                        // 缓冲区大小增加一倍线。
                        byte[] newLineBuffer = new byte[_lineBuffer.Length * 2];

                        _lineBuffer.CopyTo(newLineBuffer, 0);
                        _lineBuffer = newLineBuffer;
                    }
                }

                return Encoding.ASCII.GetString(_lineBuffer, 0, _linePosition);
            }

            public int Read(byte[] buffer, int index, int length)
            {
                int curLength = Length - Position;

                if (curLength > length)
                {
                    curLength = length;
                }

                Array.Copy(_buffer, Position, buffer, index, curLength);

                Position += curLength;

                return curLength;
            }

            #endregion
        }

        // 这个类用于下载数据压缩。
        // 他可以确定指导准确数量的字节(数据压缩).
        // 这需要对诸如流读取数据压缩的字节数据报告已经转变。
        private sealed class ZipWraperStream : Stream
        {
            #region 地板(关闭)

            private Stream _baseStream;
            private ReceiverHelper _receiverHelper;

            #endregion


            #region 性能(开放)

            public int BytesRead { get; private set; }

            public int TotalBytesRead { get; set; }

            public int LimitBytesRead { get; set; }

            #region 超定

            public override bool CanRead
            {
                get
                {
                    return _baseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return _baseStream.CanSeek;
                }
            }

            public override bool CanTimeout
            {
                get
                {
                    return _baseStream.CanTimeout;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return _baseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return _baseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _baseStream.Position;
                }
                set
                {
                    _baseStream.Position = value;
                }
            }

            #endregion

            #endregion


            public ZipWraperStream(Stream baseStream, ReceiverHelper receiverHelper)
            {
                _baseStream = baseStream;
                _receiverHelper = receiverHelper;
            }


            #region 方法(开放)

            public override void Flush()
            {
                _baseStream.Flush();
            }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // 如果发现数量位列视为字节。
                if (LimitBytesRead != 0)
                {
                    int length = LimitBytesRead - TotalBytesRead;

                    // 如果达到限额。
                    if (length == 0)
                    {
                        return 0;
                    }

                    if (length > buffer.Length)
                    {
                        length = buffer.Length;
                    }

                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, length);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, length);
                    }
                }
                else
                {
                    if (_receiverHelper.HasData)
                    {
                        BytesRead = _receiverHelper.Read(buffer, offset, count);
                    }
                    else
                    {
                        BytesRead = _baseStream.Read(buffer, offset, count);
                    }
                }

                TotalBytesRead += BytesRead;

                return BytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
            }

            #endregion
        }

        #endregion


        #region 静电场(关闭)

        private static readonly byte[] _openHtmlSignature = Encoding.ASCII.GetBytes("<html");
        private static readonly byte[] _closeHtmlSignature = Encoding.ASCII.GetBytes("</html>");

        private static readonly Regex _keepAliveTimeoutRegex = new Regex(
            @"timeout(|\s+)=(|\s+)(?<value>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _keepAliveMaxRegex = new Regex(
            @"max(|\s+)=(|\s+)(?<value>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _contentCharsetRegex = new Regex(
           @"charset(|\s+)=(|\s+)(?<value>[a-z,0-9,-]+)",
           RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion


        #region 地板(关闭)

        private readonly HttpRequest _request;
        private ReceiverHelper _receiverHelper;

        private readonly Dictionary<string, string> _headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly CookieDictionary _rawCookies = new CookieDictionary();

        #endregion


        #region 性能(开放)

        /// <summary>
        /// 返回值,指出是否发生错误时的响应的HTTP服务器。
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// 返回值,指出是否加载消息体。
        /// </summary>
        public bool MessageBodyLoaded { get; private set; }

        /// <summary>
        /// 返回值,指出是否成功完成请求(响应代码= 200 OK). 
        /// </summary>
        public bool IsOK
        {
            get
            {
                return (StatusCode == HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// 返回值指定是否转发。
        /// </summary>
        public bool HasRedirect
        {
            get
            {
                int numStatusCode = (int)StatusCode;

                if (numStatusCode >= 300 && numStatusCode < 400)
                {
                    return true;
                }

                if (_headers.ContainsKey("Location"))
                {
                    return true;
                }

                if (_headers.ContainsKey("Redirect-Location"))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 返回重新尝试次数。
        /// </summary>
        public int ReconnectCount { get; internal set; }

        #region 基本数据

        /// <summary>
        /// 互联网资源URI返回实际回答查询。
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// 返回的HTTP方法,用于获得答案。
        /// </summary>
        public HttpMethod Method { get; private set; }

        /// <summary>
        /// 返回HTTP协议版本的答案。
        /// </summary>
        public Version ProtocolVersion { get; private set; }

        /// <summary>
        /// 返回状态码答。
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// 返回地址重定向。
        /// </summary>
        /// <returns>重定向地址,否则<see langword="null"/>.</returns>
        public Uri RedirectAddress { get; private set; }

        #endregion

        #region HTTP-标题

        /// <summary>
        /// 返回消息体编码。
        /// </summary>
        /// <value>体报道,如果编码заголок给定值,否则在<see cref="xNet.Net.HttpRequest"/>. 如果没有指定,那么重要<see cref="System.Text.Encoding.Default"/>.</value>
        public Encoding CharacterSet { get; private set; }

        /// <summary>
        /// 返回消息体长度。
        /// </summary>
        /// <value>消息体长度,如果заголок预定,否则-1.</value>
        public int ContentLength { get; private set; }

        /// <summary>
        /// 返回类型的内容回答。
        /// </summary>
        /// <value>内容类型,如果答案заголок预定,否则空字符串。</value>
        public string ContentType { get; private set; }

        /// <summary>
        /// HTTP报头返回值'Location'.
        /// </summary>
        /// <returns>如果标题值заголок预定,否则空字符串。</returns>
        public string Location
        {
            get
            {
                return this["Location"];
            }
        }

        /// <summary>
        /// 返回查询结果的cookie,或安装在<see cref="xNet.Net.HttpRequest"/>.
        /// </summary>
        /// <remarks>如果厨师被发现在<see cref="xNet.Net.HttpRequest"/> 属性和值<see cref="xNet.Net.CookieDictionary.IsLocked"/> 等于<see langword="true"/>, 将创建新厨师。</remarks>
        public CookieDictionary Cookies { get; private set; }

        /// <summary>
        /// 返回时间闲置固定连接在毫秒。
        /// </summary>
        /// <value>默认值<see langword="null"/>.</value>
        public int? KeepAliveTimeout { get; private set; }

        /// <summary>
        /// 最大容许量返回一个连接请求。
        /// </summary>
        /// <value>默认值<see langword="null"/>.</value>
        public int? MaximumKeepAliveRequests { get; private set; }

        #endregion

        #endregion


        #region 索引器(开放)

        /// <summary>
        /// HTTP报头返回值。
        /// </summary>
        /// <param name="headerName">HTTP报头名称。</param>
        /// <value>HTTP报头的值,如果他问,否则空字符串。</value>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="headerName"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="headerName"/> 是空字符串。</exception>
        public string this[string headerName]
        {
            get
            {
                #region 检查参数

                if (headerName == null)
                {
                    throw new ArgumentNullException("headerName");
                }

                if (headerName.Length == 0)
                {
                    throw ExceptionHelper.EmptyString("headerName");
                }

                #endregion

                string value;

                if (!_headers.TryGetValue(headerName, out value))
                {
                    value = string.Empty;
                }

                return value;
            }
        }

        /// <summary>
        /// HTTP报头返回值。
        /// </summary>
        /// <param name="header">HTTP-标题。</param>
        /// <value>HTTP报头的值,如果他问,否则空字符串。</value>
        public string this[HttpHeader header]
        {
            get
            {
                return this[Http.Headers[header]];
            }
        }

        #endregion


        internal HttpResponse(HttpRequest request)
        {
            _request = request;

            ContentLength = -1;
            ContentType = string.Empty;
        }


        #region 方法(开放)

        /// <summary>
        /// 加载消息体和数组返回的字节。
        /// </summary>
        /// <returns>如果身体没有消息,或者他已经载入,则返回空字节数组。</returns>
        /// <exception cref="System.InvalidOperationException">方法调用的错误答案。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public byte[] ToBytes()
        {
            #region 检查状态

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return new byte[0];
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            return memoryStream.ToArray();
        }

        /// <summary>
        /// 下载并返回消息体的意思行。
        /// </summary>
        /// <returns>如果身体没有消息,或者他已经载入,它将返回空字符串。</returns>
        /// <exception cref="System.InvalidOperationException">方法调用的错误答案。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        override public string ToString()
        {
            #region 检查状态

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return string.Empty;
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;

            string text = CharacterSet.GetString(
                memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            return text;
        }

        /// <summary>
        /// 下载并保存在消息体的新文件指定路径。如果文件已经存在,则将覆盖。
        /// </summary>
        /// <param name="path">文件路径将消息保存尸体。</param>
        /// <exception cref="System.InvalidOperationException">方法调用的错误答案。</exception>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="path"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="path"/> 是空字符串只包含空格,或包含非法字符。</exception>
        /// <exception cref="System.IO.PathTooLongException">指定路径、文件名或两者最大长度超过系统规定的可能性。例如,基于Windows平台的长度不应超过248 文件名字符,不得超过260 标志。</exception>
        /// <exception cref="System.IO.FileNotFoundException">参数值<paramref name="path"/> 指向不存在的文件。</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">参数值<paramref name="path"/> 指向可望通过。</exception>
        /// <exception cref="System.IO.IOException">打开文件时发生i / o错误。</exception>
        /// <exception cref="System.Security.SecurityException">调用语句没有必要的许可。</exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// 不支持的文件读写操作当前平台。
        /// -或-
        /// 参数值<paramref name="path"/> 指定的目录。
        /// -或-
        /// 调用语句没有必要的许可。
        /// </exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public void ToFile(string path)
        {
            #region 检查状态

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            #region 检查参数

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            try
            {
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (var bytes in source)
                    {
                        fileStream.Write(bytes.Value, 0, bytes.Length);
                    }
                }
            }
            #region Catch's

            catch (ArgumentException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (NotSupportedException ex)
            {
                throw ExceptionHelper.WrongPath("path", ex);
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            #endregion

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;
        }

        /// <summary>
        /// 下载并将其返回给消息体作为流的字节内存。
        /// </summary>
        /// <returns>如果身体没有消息,或者他已经载入,它将返回值<see langword="null"/>.</returns>
        /// <exception cref="System.InvalidOperationException">方法调用的错误答案。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public MemoryStream ToMemoryStream()
        {
            #region 检查状态

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return null;
            }

            var memoryStream = new MemoryStream(
                (ContentLength == -1) ? 0 : ContentLength);

            try
            {
                IEnumerable<BytesWraper> source = GetMessageBodySource();

                foreach (var bytes in source)
                {
                    memoryStream.Write(bytes.Value, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException || ex is InvalidOperationException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                }

                throw;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }

            MessageBodyLoaded = true;
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// 透射体信息。此方法不需要应引起,如果消息正文。
        /// </summary>
        /// <exception cref="System.InvalidOperationException">方法调用的错误答案。</exception>
        /// <exception cref="xNet.Net.HttpException">错误操作的HTTP协议。</exception>
        public void None()
        {
            #region 检查状态

            if (HasError)
            {
                throw new InvalidOperationException(
                    Resources.InvalidOperationException_HttpResponse_HasError);
            }

            #endregion

            if (MessageBodyLoaded)
            {
                return;
            }

            if (ConnectionClosed())
            {
                _request.Dispose();
            }
            else
            {
                try
                {
                    IEnumerable<BytesWraper> source = GetMessageBodySource();

                    foreach (var bytes in source) { }
                }
                catch (Exception ex)
                {
                    HasError = true;

                    if (ex is IOException || ex is InvalidOperationException)
                    {
                        throw NewHttpException(Resources.HttpException_FailedReceiveMessageBody, ex);
                    }

                    throw;
                }
            }

            MessageBodyLoaded = true;
        }

        #region 厨师工作

        /// <summary>
        /// 确定是否包含指定cookie。
        /// </summary>
        /// <param name="name">库克名称。</param>
        /// <returns>意义<see langword="true"/>, 如果库克所包含或意义<see langword="false"/>.</returns>
        public bool ContainsCookie(string name)
        {
            if (Cookies == null)
            {
                return false;
            }

            return Cookies.ContainsKey(name);
        }

        /// <summary>
        /// 确定原料是否包含指定值库克。
        /// </summary>
        /// <param name="name">库克名称。</param>
        /// <returns>意义<see langword="true"/>, 如果库克所包含或意义<see langword="false"/>.</returns>
        /// <remarks>库克是在当前设置的答案。其值可用于原料获取一些额外的数据。</remarks>
        public bool ContainsRawCookie(string name)
        {
            return _rawCookies.ContainsKey(name);
        }

        /// <summary>
        /// 返回cookie值原料。
        /// </summary>
        /// <param name="name">库克名称。</param>
        /// <returns>如果给定cookie值,否则空字符串。</returns>
        /// <remarks>库克是在当前设置的答案。其值可用于原料获取一些额外的数据。</remarks>
        public string GetRawCookie(string name)
        {
            string value;

            if (!_rawCookies.TryGetValue(name, out value))
            {
                value = string.Empty;
            }

            return value;
        }

        /// <summary>
        /// 返回cookie值列出原料收藏。
        /// </summary>
        /// <returns>原料cookie值集合。</returns>
        /// <remarks>库克是在当前设置的答案。其值可用于原料获取一些额外的数据。</remarks>
        public Dictionary<string, string>.Enumerator EnumerateRawCookies()
        {
            return _rawCookies.GetEnumerator();
        }

        #endregion

        #region 标题的工作

        /// <summary>
        /// 确定是否包含指定的HTTP标题。
        /// </summary>
        /// <param name="headerName">HTTP报头名称。</param>
        /// <returns>意义<see langword="true"/>, 如果指定的HTTP标题包含值,否则<see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">参数值<paramref name="headerName"/> 等于<see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">参数值<paramref name="headerName"/> 是空字符串。</exception>
        public bool ContainsHeader(string headerName)
        {
            #region 检查参数

            if (headerName == null)
            {
                throw new ArgumentNullException("headerName");
            }

            if (headerName.Length == 0)
            {
                throw ExceptionHelper.EmptyString("headerName");
            }

            #endregion

            return _headers.ContainsKey(headerName);
        }

        /// <summary>
        /// 确定是否包含指定的HTTP标题。
        /// </summary>
        /// <param name="header">HTTP-标题。</param>
        /// <returns>意义<see langword="true"/>, 如果指定的HTTP标题包含值,否则<see langword="false"/>.</returns>
        public bool ContainsHeader(HttpHeader header)
        {
            return ContainsHeader(Http.Headers[header]);
        }

        /// <summary>
        /// 返回的HTTP标题列出收藏。
        /// </summary>
        /// <returns>HTTP报头的收藏。</returns>
        public Dictionary<string, string>.Enumerator EnumerateHeaders()
        {
            return _headers.GetEnumerator();
        }

        #endregion

        #endregion


        // 下载答案并返回响应大小的字节。
        internal long LoadResponse(HttpMethod method)
        {
            Method = method;
            Address = _request.Address;

            HasError = false;
            MessageBodyLoaded = false;
            KeepAliveTimeout = null;
            MaximumKeepAliveRequests = null;

            _headers.Clear();
            _rawCookies.Clear();

            if (_request.Cookies != null && !_request.Cookies.IsLocked)
                Cookies = _request.Cookies;
            else
                Cookies = new CookieDictionary();

            if (_receiverHelper == null)
            {
                _receiverHelper = new ReceiverHelper(
                    _request.TcpClient.ReceiveBufferSize);
            }

            _receiverHelper.Init(_request.ClientStream);

            try
            {
                ReceiveStartingLine();
                ReceiveHeaders();

                RedirectAddress = GetLocation();
                CharacterSet = GetCharacterSet();
                ContentLength = GetContentLength();
                ContentType = GetContentType();

                KeepAliveTimeout = GetKeepAliveTimeout();
                MaximumKeepAliveRequests = GetKeepAliveMax();
            }
            catch (Exception ex)
            {
                HasError = true;

                if (ex is IOException)
                {
                    throw NewHttpException(Resources.HttpException_FailedReceiveResponse, ex);
                }

                throw;
            }

            // 如果没有身体来回答信息。
            if (ContentLength == 0 ||
                Method == HttpMethod.HEAD ||
                StatusCode == HttpStatusCode.Continue ||
                StatusCode == HttpStatusCode.NoContent ||
                StatusCode == HttpStatusCode.NotModified)
            {
                MessageBodyLoaded = true;
            }

            long responseSize = _receiverHelper.Position;

            if (ContentLength > 0)
            {
                responseSize += ContentLength;
            }

            return responseSize;
        }


        #region 方法(关闭)

        #region 数据加载

        private void ReceiveStartingLine()
        {
            string startingLine;

            while (true)
            {
                startingLine = _receiverHelper.ReadLine();

                if (startingLine.Length == 0)
                {
                    HttpException exception =
                        NewHttpException(Resources.HttpException_ReceivedEmptyResponse);

                    exception.EmptyMessageBody = true;

                    throw exception;
                }
                else if (startingLine == Http.NewLine)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            string version = startingLine.Substring("HTTP/", " ");
            string statusCode = startingLine.Substring(" ", " ");

            if (statusCode.Length == 0)
            {
                // Если сервер не возвращает Reason Phrase
                statusCode = startingLine.Substring(" ", Http.NewLine);
            }

            if (version.Length == 0 || statusCode.Length == 0)
            {
                throw NewHttpException(Resources.HttpException_ReceivedEmptyResponse);
            }

            ProtocolVersion = Version.Parse(version);

            StatusCode = (HttpStatusCode)Enum.Parse(
                typeof(HttpStatusCode), statusCode);
        }

        private void SetCookie(string value)
        {
            if (value.Length == 0)
            {
                return;
            }

            // 找位置,结束并开始描述他库克参数。
            int endCookiePos = value.IndexOf(';');

            // 名字和值之间的位置找厨师。
            int separatorPos = value.IndexOf('=');

            if (separatorPos == -1)
            {
                string message = string.Format(
                    Resources.HttpException_WrongCookie, value, Address.Host);

                throw NewHttpException(message);
            }

            string cookieValue;
            string cookieName = value.Substring(0, separatorPos);

            if (endCookiePos == -1)
            {
                cookieValue = value.Substring(separatorPos + 1);
            }
            else
            {
                cookieValue = value.Substring(separatorPos + 1,
                    (endCookiePos - separatorPos) - 1);

                #region 收到的时间是会做饭

                int expiresPos = value.IndexOf("expires=");

                if (expiresPos != -1)
                {
                    string expiresStr;
                    int endExpiresPos = value.IndexOf(';', expiresPos);

                    expiresPos += 8;

                    if (endExpiresPos == -1)
                    {
                        expiresStr = value.Substring(expiresPos);
                    }
                    else
                    {
                        expiresStr = value.Substring(expiresPos, endExpiresPos - expiresPos);
                    }

                    DateTime expires;

                    // 如果时间到了,她删除cookie。
                    if (DateTime.TryParse(expiresStr, out expires) &&
                        expires < DateTime.Now)
                    {
                        Cookies.Remove(cookieName);
                    }
                }

                #endregion
            }

            // 如果需要删除cookie。
            if (cookieValue.Length == 0 ||
                cookieValue.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                Cookies.Remove(cookieName);
            }
            else
            {
                Cookies[cookieName] = cookieValue;
            }

            _rawCookies[cookieName] = value;
        }

        private void ReceiveHeaders()
        {
            while (true)
            {
                string header = _receiverHelper.ReadLine();

                // 如果达到端标题。
                if (header == Http.NewLine)
                    return;

                // 名字和值之间寻找位置标题。
                int separatorPos = header.IndexOf(':');

                if (separatorPos == -1)
                {
                    string message = string.Format(
                        Resources.HttpException_WrongHeader, header, Address.Host);

                    throw NewHttpException(message);
                }

                string headerName = header.Substring(0, separatorPos);
                string headerValue = header.Substring(separatorPos + 1).Trim(' ', '\t', '\r', '\n');

                if (headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    SetCookie(headerValue);
                }
                else
                {
                    _headers[headerName] = headerValue;
                }
            }
        }

        #endregion

        #region 加载消息体

        private IEnumerable<BytesWraper> GetMessageBodySource()
        {
            if (_headers.ContainsKey("Content-Encoding"))
            {
                return GetMessageBodySourceZip();
            }

            return GetMessageBodySourceStd();
        }

        // 普通下载数据。
        private IEnumerable<BytesWraper> GetMessageBodySourceStd()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunked();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBody(ContentLength);
            }

            return ReceiveMessageBody(_request.ClientStream);
        }

        // 下载数据压缩。
        private IEnumerable<BytesWraper> GetMessageBodySourceZip()
        {
            if (_headers.ContainsKey("Transfer-Encoding"))
            {
                return ReceiveMessageBodyChunkedZip();
            }

            if (ContentLength != -1)
            {
                return ReceiveMessageBodyZip(ContentLength);
            }

            var streamWrapper = new ZipWraperStream(
                _request.ClientStream, _receiverHelper);

            return ReceiveMessageBody(GetZipStream(streamWrapper));
        }

        // 加载消息体长度未知。
        private IEnumerable<BytesWraper> ReceiveMessageBody(Stream stream)
        {
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            int begBytesRead = 0;

            // 读取原始数据从消息体。
            if (stream is GZipStream || stream is DeflateStream)
            {
                begBytesRead = stream.Read(buffer, 0, bufferSize);
            }
            else
            {
                if (_receiverHelper.HasData)
                {
                    begBytesRead = _receiverHelper.Read(buffer, 0, bufferSize);
                }

                if (begBytesRead < bufferSize)
                {
                    begBytesRead += stream.Read(buffer, begBytesRead, bufferSize - begBytesRead);
                }
            }

            // 返回原始数据。
            bytesWraper.Length = begBytesRead;
            yield return bytesWraper;

            // 检查标签是否打开'<html'.
            // 如果有,则读取数据,直到遇见不关闭龙舌兰酒'</html>'.
            bool isHtml = FindSignature(buffer, begBytesRead, _openHtmlSignature);

            if (isHtml)
            {
                bool found = FindSignature(buffer, begBytesRead, _closeHtmlSignature);

                // 检查是否在初始数据的结束标记。
                if (found)
                {
                    yield break;
                }
            }

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, bufferSize);

                // 如果消息正文是HTML。
                if (isHtml)
                {
                    if (bytesRead == 0)
                    {
                        WaitData();

                        continue;
                    }

                    bool found = FindSignature(buffer, bytesRead, _closeHtmlSignature);

                    if (found)
                    {
                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;

                        yield break;
                    }
                }
                else if (bytesRead == 0)
                {
                    yield break;
                }

                bytesWraper.Length = bytesRead;
                yield return bytesWraper;
            }
        }

        // 消息体加载已知长度。
        private IEnumerable<BytesWraper> ReceiveMessageBody(int contentLength)
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            int totalBytesRead = 0;

            while (totalBytesRead != contentLength)
            {
                int bytesRead;

                if (_receiverHelper.HasData)
                {
                    bytesRead = _receiverHelper.Read(buffer, 0, bufferSize);
                }
                else
                {
                    bytesRead = stream.Read(buffer, 0, bufferSize);
                }

                if (bytesRead == 0)
                {
                    WaitData();
                }
                else
                {
                    totalBytesRead += bytesRead;

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        // 加载消息体部分。
        private IEnumerable<BytesWraper> ReceiveMessageBodyChunked()
        {
            Stream stream = _request.ClientStream;
            var bytesWraper = new BytesWraper();

            int bufferSize = _request.TcpClient.ReceiveBufferSize;
            byte[] buffer = new byte[bufferSize];

            bytesWraper.Value = buffer;

            while (true)
            {
                string line = _receiverHelper.ReadLine();

                // 如果达成结束块。
                if (line == Http.NewLine)
                    continue;

                line = line.Trim(' ', '\r', '\n');

                // 如果消息正文结局达成。
                if (line == string.Empty)
                    yield break;

                int blockLength;
                int totalBytesRead = 0;

                #region 问块长度

                try
                {
                    blockLength = Convert.ToInt32(line, 16);
                }
                catch (Exception ex)
                {
                    if (ex is FormatException || ex is OverflowException)
                    {
                        throw NewHttpException(string.Format(
                            Resources.HttpException_WrongChunkedBlockLength, line), ex);
                    }

                    throw;
                }

                #endregion

                // 如果消息正文结局达成。
                if (blockLength == 0)
                    yield break;

                while (totalBytesRead != blockLength)
                {
                    int length = blockLength - totalBytesRead;

                    if (length > bufferSize)
                    {
                        length = bufferSize;
                    }

                    int bytesRead;

                    if (_receiverHelper.HasData)
                    {
                        bytesRead = _receiverHelper.Read(buffer, 0, length);
                    }
                    else
                    {
                        bytesRead = stream.Read(buffer, 0, length);
                    }

                    if (bytesRead == 0)
                    {
                        WaitData();
                    }
                    else
                    {
                        totalBytesRead += bytesRead;

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyZip(int contentLength)
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream(
                _request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, bufferSize);

                    if (bytesRead == 0)
                    {
                        if (streamWrapper.TotalBytesRead == contentLength)
                        {
                            yield break;
                        }
                        else
                        {
                            WaitData();

                            continue;
                        }
                    }

                    bytesWraper.Length = bytesRead;
                    yield return bytesWraper;
                }
            }
        }

        private IEnumerable<BytesWraper> ReceiveMessageBodyChunkedZip()
        {
            var bytesWraper = new BytesWraper();
            var streamWrapper = new ZipWraperStream
                (_request.ClientStream, _receiverHelper);

            using (Stream stream = GetZipStream(streamWrapper))
            {
                int bufferSize = _request.TcpClient.ReceiveBufferSize;
                byte[] buffer = new byte[bufferSize];

                bytesWraper.Value = buffer;

                while (true)
                {
                    string line = _receiverHelper.ReadLine();

                    // 如果达成结束块。
                    if (line == Http.NewLine)
                        continue;

                    line = line.Trim(' ', '\r', '\n');

                    // 如果消息正文结局达成。
                    if (line == string.Empty)
                        yield break;

                    int blockLength;

                    #region 问块长度

                    try
                    {
                        blockLength = Convert.ToInt32(line, 16);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FormatException || ex is OverflowException)
                        {
                            throw NewHttpException(string.Format(
                                Resources.HttpException_WrongChunkedBlockLength, line), ex);
                        }

                        throw;
                    }

                    #endregion

                    // 如果消息正文结局达成。
                    if (blockLength == 0)
                        yield break;

                    streamWrapper.TotalBytesRead = 0;
                    streamWrapper.LimitBytesRead = blockLength;

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, bufferSize);

                        if (bytesRead == 0)
                        {
                            if (streamWrapper.TotalBytesRead == blockLength)
                            {
                                break;
                            }
                            else
                            {
                                WaitData();

                                continue;
                            }
                        }

                        bytesWraper.Length = bytesRead;
                        yield return bytesWraper;
                    }
                }
            }
        }

        #endregion

        #region 收到HTTP报头的值

        private bool ConnectionClosed()
        {
            if (_headers.ContainsKey("Connection") &&
                _headers["Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_headers.ContainsKey("Proxy-Connection") &&
                _headers["Proxy-Connection"].Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private int? GetKeepAliveTimeout()
        {
            if (!_headers.ContainsKey("Keep-Alive"))
                return null;

            var header = _headers["Keep-Alive"];
            var match = _keepAliveTimeoutRegex.Match(header);

            if (match.Success)
                return int.Parse(match.Groups["value"].Value) * 1000; // 以毫秒为单位。

            return null;
        }

        private int? GetKeepAliveMax()
        {
            if (!_headers.ContainsKey("Keep-Alive"))
                return null;

            var header = _headers["Keep-Alive"];
            var match = _keepAliveMaxRegex.Match(header);

            if (match.Success)
                return int.Parse(match.Groups["value"].Value);

            return null;
        }

        private Uri GetLocation()
        {
            string location;

            if (!_headers.TryGetValue("Location", out location))
                _headers.TryGetValue("Redirect-Location", out location);

            if (string.IsNullOrEmpty(location))
                return null;

            Uri redirectAddress;
            var baseAddress = _request.Address;
            Uri.TryCreate(baseAddress, location, out redirectAddress);

            return redirectAddress;
        }

        private Encoding GetCharacterSet()
        {
            if (!_headers.ContainsKey("Content-Type"))
                return _request.CharacterSet ?? Encoding.Default;

            var header = _headers["Content-Type"];
            var match = _contentCharsetRegex.Match(header);

            if (!match.Success)
                return _request.CharacterSet ?? Encoding.Default;

            var charset = match.Groups["value"];

            try
            {
                return Encoding.GetEncoding(charset.Value);
            }
            catch (ArgumentException ex)
            {
                return _request.CharacterSet ?? Encoding.Default;
            }
        }

        private int GetContentLength()
        {
            if (_headers.ContainsKey("Content-Length"))
            {
                int contentLength;
                int.TryParse(_headers["Content-Length"], out contentLength);
                return contentLength;
            }

            return -1;
        }

        private string GetContentType()
        {
            if (_headers.ContainsKey("Content-Type"))
            {
                string contentType = _headers["Content-Type"];

                // 找位置,结束并开始描述描述内容类型的参数。
                int endTypePos = contentType.IndexOf(';');
                if (endTypePos != -1)
                    contentType = contentType.Substring(0, endTypePos);
  
                return contentType;
            }

            return string.Empty;
        }

        #endregion

        private void WaitData()
        {
            int sleepTime = 0;
            int delay = (_request.TcpClient.ReceiveTimeout < 10) ?
                10 : _request.TcpClient.ReceiveTimeout;

            while (!_request.ClientNetworkStream.DataAvailable)
            {
                if (sleepTime >= delay)
                {
                    throw NewHttpException(Resources.HttpException_WaitDataTimeout);
                }

                sleepTime += 10;
                Thread.Sleep(10);
            }
        }

        private Stream GetZipStream(Stream stream)
        {
            string contentEncoding = _headers["Content-Encoding"].ToLower();

            switch (contentEncoding)
            {
                case "gzip":
                    return new GZipStream(stream, CompressionMode.Decompress, true);

                case "deflate":
                    return new DeflateStream(stream, CompressionMode.Decompress, true);

                default:
                    throw new InvalidOperationException(string.Format(
                        Resources.InvalidOperationException_NotSupportedEncodingFormat, contentEncoding));
            }
        }

        private bool FindSignature(byte[] source, int sourceLength, byte[] signature)
        {
            int length = (sourceLength - signature.Length) + 1;

            for (int sourceIndex = 0; sourceIndex < length; ++sourceIndex)
            {
                for (int signatureIndex = 0; signatureIndex < signature.Length; ++signatureIndex)
                {
                    byte sourceByte = source[signatureIndex + sourceIndex];
                    char sourceChar = (char)sourceByte;

                    if (char.IsLetter(sourceChar))
                    {
                        sourceChar = char.ToLower(sourceChar);
                    }

                    sourceByte = (byte)sourceChar;

                    if (sourceByte != signature[signatureIndex])
                    {
                        break;
                    }
                    else if (signatureIndex == (signature.Length - 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private HttpException NewHttpException(string message, Exception innerException = null)
        {
            return new HttpException(string.Format(message, Address.Host),
                HttpExceptionStatus.ReceiveFailure, HttpStatusCode.None, innerException);
        }

        #endregion
    }
}