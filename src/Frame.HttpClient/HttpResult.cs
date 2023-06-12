using System.Net;
using System.Net.Http;

namespace Frame.HttpClient
{
    public class HttpResult
    {
        /// <summary>
        /// 状态
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        public HttpContent? Content { get; set; }
    }
}
