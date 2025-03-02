using Microsoft.Extensions.DependencyInjection;

namespace DHttpClient.Extensions
{
    namespace DHttpClient.Extensions
    {
        public static class DHttpClientServiceExtensions
        {
            /// <summary>
            /// Registers the DHttpClient services so that IHttpRequestBuilder can be injected.
            /// </summary>
            public static IServiceCollection AddDHttpClient(this IServiceCollection services)
            {
                // Registers IHttpRequestBuilder with an HttpClient injected via IHttpClientFactory.
                services.AddHttpClient<IHttpRequestBuilder, HttpRequestBuilder>();
                return services;
            }
        }
    }
}