using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using TimeZoneConvertorLibrary.Services;
using TimeZoneConvertorLibrary.Interfaces;

namespace TimeZoneConvertorLibrary.Extensions
{
    /// <summary>
    /// Extension methods for configuring dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all timezone conversion services to the DI container
        /// </summary>
        public static IServiceCollection AddTimeZoneConversionServices(this IServiceCollection services)
        {
            // Register NodaTime provider
            services.AddSingleton<IDateTimeZoneProvider>(DateTimeZoneProviders.Tzdb);

            // Register our services
            services.AddScoped<ITimeZoneValidationService, TimeZoneValidationService>();
            services.AddScoped<ITimeZoneConversionService, TimeZoneConversionService>();

            // Ensure logging is available
            services.AddLogging();

            return services;
        }

        /// <summary>
        /// Adds timezone conversion services with custom logger factory
        /// </summary>
        public static IServiceCollection AddTimeZoneConversionServices(
            this IServiceCollection services,
            ILoggerFactory loggerFactory)
        {
            services.AddSingleton(loggerFactory);
            return services.AddTimeZoneConversionServices();
        }


        /// <summary>
        /// Adds timezone conversion services as singletons for better performance
        /// </summary>
        public static IServiceCollection AddTimeZoneConversionSingletonServices(this IServiceCollection services)
        {
            // Register NodaTime provider
            services.AddSingleton<IDateTimeZoneProvider>(DateTimeZoneProviders.Tzdb);

            // Register our services as singletons
            services.AddSingleton<ITimeZoneValidationService, TimeZoneValidationService>();
            services.AddSingleton<ITimeZoneConversionService, TimeZoneConversionService>();

            // Ensure logging is available
            services.AddLogging();

            return services;
        }

    }
}