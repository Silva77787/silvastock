using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace SilvaStock.Api.Cors;

/// <summary>CORS restricted to the configured site origins, with credentials (cookie session).</summary>
internal static class CorsExtensions
{
    private static readonly string[] _allowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiCorsOptions>()
            .Bind(configuration.GetSection(ApiCorsOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ApiCorsOptions>, ApiCorsOptionsValidator>();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<ApiCorsOptions>>((cors, apiCors) =>
                cors.AddDefaultPolicy(policy => policy
                    .WithOrigins([.. apiCors.Value.AllowedOrigins.Select(o => o.TrimEnd('/'))])
                    .WithMethods(_allowedMethods)
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        return services;
    }
}
