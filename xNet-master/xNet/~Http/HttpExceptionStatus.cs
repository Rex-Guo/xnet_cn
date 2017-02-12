
namespace xNet
{
    /// <summary>
    /// 定义类的状态<see cref="HttpException"/>.
    /// </summary>
    public enum HttpExceptionStatus
    {
        /// <summary>
        /// 发生错误。
        /// </summary>
        Other,
        /// <summary>
        /// 答案是通过从服务器完成,但诤协议级。假设服务器返回错误404 или未找到("没有找到").
        /// </summary>
        ProtocolError,
        /// <summary>
        /// 无法连接到HTTP服务器。
        /// </summary>
        ConnectFailure,
        /// <summary>
        /// 无法发送请求的HTTP服务器。
        /// </summary>
        SendFailure,
        /// <summary>
        /// 未能下载HTTP服务器的响应。
        /// </summary>
        ReceiveFailure
    }
}