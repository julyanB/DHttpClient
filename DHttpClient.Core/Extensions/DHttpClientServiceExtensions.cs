using System.Net.Http;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace DHttpClient.Extensions
{
    public static class DHttpClientServiceExtensions
    {
        /// <summary>
        /// Registers DHttpClient services using IHttpClientFactory for proper HttpClient lifecycle management.
        /// This allows injecting IDHttpClient into your services.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddDHttpClient(this IServiceCollection services)
        {
            // Registers IDHttpClient with an HttpClient injected via IHttpClientFactory.
            // DHttpClient receives the HttpClient and sets disposeHttpClient to false.
            services.AddHttpClient<IDHttpClient, DHttpClient>();
            return services;
        }

        /// <summary>
        /// Registers a named DHttpClient using IHttpClientFactory.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="name">The logical name of the client.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddDHttpClient(this IServiceCollection services, string name)
        {
             return services.AddHttpClient<IDHttpClient, DHttpClient>(name);
        }

         /// <summary>
        /// Registers a typed DHttpClient using IHttpClientFactory with configuration.
        /// </summary>
        /// <typeparam name="TClient">The type of the client interface (e.g., IMyApiClient).</typeparam>
        /// <typeparam name="TImplementation">The implementation type inheriting from DHttpClient.</typeparam>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="configureClient">A delegate to configure the underlying HttpClient.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddDHttpClient<TClient, TImplementation>(
            this IServiceCollection services, Action<HttpClient> configureClient)
            where TClient : class, IDHttpClient
            where TImplementation : class, TClient
        {
            return services.AddHttpClient<TClient, TImplementation>().ConfigureHttpClient(configureClient);
        }
    }
}