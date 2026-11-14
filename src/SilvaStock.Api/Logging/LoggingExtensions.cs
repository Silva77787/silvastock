using System.Text.Encodings.Web;
using System.Text.Json;

namespace SilvaStock.Api.Logging;

/// <summary>Structured logging with the built-in providers only.</summary>
internal static class LoggingExtensions
{
    /// <summary>
    /// Replaces the default providers with a single JSON console provider: one JSON object per line,
    /// UTC ISO-8601 timestamps and logging scopes (request id, trace id, …) included.
    /// </summary>
    public static ILoggingBuilder AddApiJsonLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            // Relaxed escaping keeps quotes and non-ASCII (pt-PT) readable; logs go to stdout, never into HTML.
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        });

        return logging;
    }
}
