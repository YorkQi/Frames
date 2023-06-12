using Frame.HttpClient;
using Newtonsoft.Json;
using System.IO;

namespace HttpClient
{
    public static class HttpResultExtensions
    {
        public static T ContentAsType<T>(this HttpResult response) where T : class, new()
        {
            if (response.Content != null)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                if (!string.IsNullOrEmpty(data))
                {
                    return JsonConvert.DeserializeObject<T>(data) ?? new T();
                }
            }
            return new T();
        }

        public static string ContentAsJson(this HttpResult response)
        {
            if (response.Content == null) return string.Empty;
            return JsonConvert.SerializeObject(response.Content.ReadAsStringAsync().Result);
        }

        public static string ContentAsString(this HttpResult response)
        {
            if (response.Content == null) return string.Empty;
            return response.Content.ReadAsStringAsync().Result;
        }
        public static byte[]? ContentAsByteArray(this HttpResult response)
        {
            if (response.Content == null) return null;
            return response.Content.ReadAsByteArrayAsync().Result;
        }
        public static Stream? ContentAsStream(this HttpResult response)
        {
            if (response.Content == null) return null;
            return response.Content.ReadAsStreamAsync().Result;
        }
    }
}
