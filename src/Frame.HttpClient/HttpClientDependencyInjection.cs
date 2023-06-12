using Frame.HttpClient;
using Frame.HttpClient.HttpRequests;
using HttpClient;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientDependencyInjection
    {
        public static IServiceCollection AddFrameHttpClient(this IServiceCollection services)
        {
            services.AddSingleton<IHttpClient, HttpRequestClient>();
            services.AddSingleton<IHttpRequestFactory, HttpRequestFactory>();
            return services;
        }
    }
}
