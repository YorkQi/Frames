using Frame.HttpClient;
using Frame.HttpClient.HttpRequests;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpClient
{
    public class HttpRequestFactory : IHttpRequestFactory
    {
        public async Task<HttpResult> GetAsync(string requestUri)
            => await GetAsync(requestUri, "");

        public async Task<HttpResult> GetAsync(string requestUri, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Get)
                                .AddRequestUri(requestUri)
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public HttpResult Get(string requestUri, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Get)
                                .AddRequestUri(requestUri)
                                .AddToken(token, tokenScheme);
            return builder.Send();
        }

        public HttpResult Get(string requestUri, TimeSpan timeout)
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Get)
                                .AddRequestUri(requestUri)
                                .AddTimeout(timeout);
            return builder.Send();
        }

        public HttpResult Get(string requestUri)
              => Get(requestUri, "");

        public async Task<HttpResult> PostAsync(string requestUri, object value)
            => await PostAsync(requestUri, value, "");

        public async Task<HttpResult> PostAsync(
            string requestUri, object value, string acceptHeader, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(new JsonContent(value))
                                .AddToken(token, tokenScheme)
                                .AddAcceptHeader(acceptHeader);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PostAsync(
            string requestUri, object value, string acceptHeader, object content, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(new JsonContent(value))
                                .AddToken(token, tokenScheme)
                                .AddAcceptHeader(acceptHeader);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PostAsync(
            string requestUri, object value, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(new JsonContent(value))
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PostAsync(
                string requestUri, object value, TimeSpan timeSpan, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(new JsonContent(value))
                                .AddToken(token, tokenScheme)
                                .AddTimeout(timeSpan);

            return await builder.SendAsync();
        }


        public async Task<HttpResult> PutAsync(string requestUri, object value)
            => await PutAsync(requestUri, value, "");

        public async Task<HttpResult> PutAsync(
            string requestUri, object value, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Put)
                                .AddRequestUri(requestUri)
                                .AddContent(new JsonContent(value))
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PatchAsync(string requestUri, object value)
            => await PatchAsync(requestUri, value, "");

        public async Task<HttpResult> PatchAsync(
            string requestUri, object value, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(new HttpMethod("PATCH"))
                                .AddRequestUri(requestUri)
                                .AddContent(new PatchContent(value))
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> DeleteAsync(string requestUri)
            => await DeleteAsync(requestUri, "");

        public async Task<HttpResult> DeleteAsync(
            string requestUri, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Delete)
                                .AddRequestUri(requestUri)
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PostFileAsync(string requestUri,
            string filePath, string apiParamName)
            => await PostFileAsync(requestUri, filePath, apiParamName, "");

        public async Task<HttpResult> PostFileAsync(string requestUri,
            string filePath, string apiParamName, string token, string tokenScheme = "Bearer")
        {
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(new FileContent(filePath, apiParamName))
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }

        public async Task<HttpResult> PostFile(string requestUri, Stream file, string apiParamName)
        => await PostFileAsync(requestUri, file, apiParamName, "");

        public async Task<HttpResult> PostFileAsync(string requestUri, Stream file, string apiParamName, string token, string tokenScheme = "Bearer")
        {
            var fromData = new MultipartFormDataContent();
            fromData.Add(new StreamContent(file), apiParamName);
            var builder = new HttpRequestClient()
                                .AddMethod(HttpMethod.Post)
                                .AddRequestUri(requestUri)
                                .AddContent(fromData)
                                .AddToken(token, tokenScheme);

            return await builder.SendAsync();
        }
    }
}
