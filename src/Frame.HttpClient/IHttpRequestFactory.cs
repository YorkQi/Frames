using Frame.HttpClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HttpClient
{
    public interface IHttpRequestFactory
    {
        Task<HttpResult> GetAsync(string requestUri);


        Task<HttpResult> GetAsync(string requestUri, string token, string tokenScheme = "Bearer");


        HttpResult Get(string requestUri, string token, string tokenScheme = "Bearer");


        HttpResult Get(string requestUri, TimeSpan timeout);


        HttpResult Get(string requestUri);

        Task<HttpResult> PostAsync(string requestUri, object value);

        Task<HttpResult> PostAsync(
          string requestUri, object value, string acceptHeader, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PostAsync(
          string requestUri, object value, string acceptHeader, object content, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PostAsync(
          string requestUri, object value, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PostAsync(
              string requestUri, object value, TimeSpan timeSpan, string token, string tokenScheme = "Bearer");



        Task<HttpResult> PutAsync(string requestUri, object value);

        Task<HttpResult> PutAsync(
          string requestUri, object value, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PatchAsync(string requestUri, object value);

        Task<HttpResult> PatchAsync(
          string requestUri, object value, string token, string tokenScheme = "Bearer");


        Task<HttpResult> DeleteAsync(string requestUri);

        Task<HttpResult> DeleteAsync(
          string requestUri, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PostFileAsync(string requestUri,
          string filePath, string apiParamName);

        Task<HttpResult> PostFileAsync(string requestUri,
          string filePath, string apiParamName, string token, string tokenScheme = "Bearer");


        Task<HttpResult> PostFile(string requestUri, Stream file, string apiParamName);

        Task<HttpResult> PostFileAsync(string requestUri, Stream file, string apiParamName, string token, string tokenScheme = "Bearer");

    }
}
