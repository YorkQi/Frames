using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Frame.HttpClient.HttpRequests
{
    public class HttpRequestClient : IHttpClient
    {
        private HttpMethod? method = null;
        private string requestUri = "";
        private HttpContent? content = null;
        private string scheme = "";
        private string token = "";
        private string acceptHeader = "application/json";
        private TimeSpan timeout = new TimeSpan(0, 0, 30);
        private bool allowAutoRedirect = false;

        public HttpRequestClient()
        {
        }

        public IHttpClient AddMethod(HttpMethod method)
        {
            this.method = method;
            return this;
        }

        public IHttpClient AddRequestUri(string requestUri)
        {
            this.requestUri = requestUri;
            return this;
        }

        public IHttpClient AddContent(HttpContent content)
        {
            this.content = content;
            return this;
        }

        public IHttpClient AddToken(string token, string scheme = "Bearer")
        {
            this.token = token;
            this.scheme = scheme;
            return this;
        }

        public IHttpClient AddAcceptHeader(string acceptHeader)
        {
            this.acceptHeader = acceptHeader;
            return this;
        }

        public IHttpClient AddTimeout(TimeSpan timeout)
        {
            this.timeout = timeout;
            return this;
        }

        public IHttpClient AddAllowAutoRedirect(bool allowAutoRedirect)
        {
            this.allowAutoRedirect = allowAutoRedirect;
            return this;
        }

        public async Task<HttpResult> SendAsync()
        {
            EnsureArguments();

            var request = new HttpRequestMessage
            {
                Method = this.method,
                RequestUri = new Uri(this.requestUri)
            };

            if (this.content != null)
                request.Content = this.content;

            if (!string.IsNullOrEmpty(this.token) && !string.IsNullOrEmpty(this.scheme))
                request.Headers.Authorization = new AuthenticationHeaderValue(this.scheme, this.token);

            request.Headers.Accept.Clear();
            if (!string.IsNullOrEmpty(this.acceptHeader))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(this.acceptHeader));

            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = this.allowAutoRedirect;

            var client = new System.Net.Http.HttpClient(handler);
            client.Timeout = this.timeout;

            var result = await client.SendAsync(request);
            return new HttpResult()
            {
                StatusCode = result.StatusCode,
                Message = result.RequestMessage.ToString(),
                Content = result.Content
            };
        }

        public HttpResult Send()
        {
            EnsureArguments();

            var request = new HttpRequestMessage
            {
                Method = this.method,
                RequestUri = new Uri(this.requestUri)
            };

            if (this.content != null)
                request.Content = this.content;

            if (!string.IsNullOrEmpty(this.token) && !string.IsNullOrEmpty(this.scheme))
                request.Headers.Authorization = new AuthenticationHeaderValue(this.scheme, this.token);

            request.Headers.Accept.Clear();
            if (!string.IsNullOrEmpty(this.acceptHeader))
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(this.acceptHeader));

            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = this.allowAutoRedirect;

            var client = new System.Net.Http.HttpClient(handler);
            client.Timeout = this.timeout;
            var result = client.SendAsync(request).GetAwaiter().GetResult();
            return new HttpResult()
            {
                StatusCode = result.StatusCode,
                Message = result.RequestMessage.ToString(),
                Content = result.Content
            };
        }


        #region  Private 

        private void EnsureArguments()
        {
            if (this.method == null)
                throw new ArgumentNullException("Method");

            if (string.IsNullOrEmpty(this.requestUri))
                throw new ArgumentNullException("Request Uri");
        }

        #endregion
    }
}
