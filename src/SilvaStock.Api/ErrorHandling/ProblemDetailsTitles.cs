using Microsoft.AspNetCore.WebUtilities;

namespace SilvaStock.Api.ErrorHandling;

/// <summary>
/// Portuguese (pt-PT) titles for framework-generated ProblemDetails. Titles set explicitly by our own code
/// are left untouched; only missing titles or the framework's English defaults are replaced.
/// </summary>
internal static class ProblemDetailsTitles
{
    // Framework default title for 500 (differs from the reason phrase).
    private const string _frameworkDefault500Title = "An error occurred while processing your request.";

    private static readonly Dictionary<int, string> _titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Pedido inválido.",
        [StatusCodes.Status401Unauthorized] = "É necessário iniciar sessão.",
        [StatusCodes.Status403Forbidden] = "Não tem permissão para aceder a este recurso.",
        [StatusCodes.Status404NotFound] = "Recurso não encontrado.",
        [StatusCodes.Status405MethodNotAllowed] = "Método não permitido.",
        [StatusCodes.Status406NotAcceptable] = "Formato de resposta não suportado.",
        [StatusCodes.Status408RequestTimeout] = "O pedido excedeu o tempo limite.",
        [StatusCodes.Status409Conflict] = "O pedido entra em conflito com o estado atual do recurso.",
        [StatusCodes.Status413PayloadTooLarge] = "O pedido é demasiado grande.",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de conteúdo não suportado.",
        [StatusCodes.Status422UnprocessableEntity] = "Não foi possível processar o pedido.",
        [StatusCodes.Status429TooManyRequests] = "Demasiados pedidos. Tente novamente mais tarde.",
        [StatusCodes.Status500InternalServerError] = "Ocorreu um erro inesperado.",
        [StatusCodes.Status502BadGateway] = "Erro ao comunicar com um serviço externo.",
        [StatusCodes.Status503ServiceUnavailable] = "Serviço temporariamente indisponível.",
        [StatusCodes.Status504GatewayTimeout] = "Um serviço externo não respondeu a tempo.",
    };

    /// <summary>Returns the pt-PT title to use, or the current title if it was set explicitly.</summary>
    public static string? Localize(string? currentTitle, int statusCode)
    {
        if (!_titles.TryGetValue(statusCode, out var portuguese))
        {
            return currentTitle;
        }

        var isFrameworkDefault = currentTitle is null
            || string.Equals(currentTitle, ReasonPhrases.GetReasonPhrase(statusCode), StringComparison.Ordinal)
            || (statusCode == StatusCodes.Status500InternalServerError
                && string.Equals(currentTitle, _frameworkDefault500Title, StringComparison.Ordinal));

        return isFrameworkDefault ? portuguese : currentTitle;
    }
}
