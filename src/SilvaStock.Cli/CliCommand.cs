namespace SilvaStock.Cli;

/// <summary>A parsed CLI command.</summary>
internal abstract record CliCommand
{
    /// <summary><c>help</c>, <c>--help</c> or <c>-h</c>: prints the usage.</summary>
    public sealed record Help : CliCommand;

    /// <summary><c>add-admin &lt;email&gt; [--name "Name"]</c>.</summary>
    public sealed record AddAdmin(string Email, string? DisplayName) : CliCommand;

    /// <summary><c>remove-admin &lt;email&gt;</c>.</summary>
    public sealed record RemoveAdmin(string Email) : CliCommand;

    /// <summary><c>reset-google &lt;email&gt;</c>.</summary>
    public sealed record ResetGoogle(string Email) : CliCommand;

    /// <summary><c>list-admins</c>.</summary>
    public sealed record ListAdmins : CliCommand;
}

/// <summary>Result of parsing: either a command or a usage error message.</summary>
internal sealed record ParseResult(CliCommand? Command, string? Error)
{
    public static ParseResult Ok(CliCommand command) => new(command, null);

    public static ParseResult Fail(string error) => new(null, error);
}
