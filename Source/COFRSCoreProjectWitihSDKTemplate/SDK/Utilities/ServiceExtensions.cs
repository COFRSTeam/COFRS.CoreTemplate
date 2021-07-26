using $safeprojectname$.Contracts;
using $safeprojectname$.Facades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Linq;
using System.Text.Json;

namespace $safeprojectname$.Utilities
{
    public static class ServicesExtension
    {
    /// <summary>
    /// Registers the $saferootprojectname$ SDK with the dependency injection system
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <param name="baseUrl">The base <see cref="Uri"/> of the service to be called.</param>
    /// <param name="supportedMediaTypes">The list of media types that the service supports</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> that control how json is to be serialized and deserialized.</param>
    public static IServiceCollection Use$saferootprojectname$SDK(this IServiceCollection services, Uri baseUrl, string[] supportedMediaTypes, JsonSerializerOptions options)
        {
            if (!services.Any(x => x.ServiceType == typeof(ObjectPoolProvider)))
                services.AddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());

            if (!services.Any(x => x.ServiceType == typeof(JsonSerializerOptions)))
                services.AddSingleton<JsonSerializerOptions>(options);

            var sdkOptions = new SDKOptions() { SupportedMediaTypes = supportedMediaTypes };
            services.AddSingleton<SDKOptions>(sdkOptions);

            services.AddHttpClient<I$saferootprojectname$HealthCheck, $saferootprojectname$HealthCheck > (client =>
            {
                client.BaseAddress = baseUrl;
            });

            return services;
    }
}
}
