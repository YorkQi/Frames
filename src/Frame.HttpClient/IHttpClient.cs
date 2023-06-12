using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Frame.HttpClient
{
    public interface IHttpClient
    {
        IHttpClient AddMethod(HttpMethod method);


        IHttpClient AddRequestUri(string requestUri);


        IHttpClient AddContent(HttpContent content);


        IHttpClient AddToken(string token, string scheme = "Bearer");


        IHttpClient AddAcceptHeader(string acceptHeader);


        IHttpClient AddTimeout(TimeSpan timeout);


        IHttpClient AddAllowAutoRedirect(bool allowAutoRedirect);


        Task<HttpResult> SendAsync();


        HttpResult Send();

    }
}
