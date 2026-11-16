namespace SilvaStock.Cli;

/// <summary>Process exit codes of the <c>silvastock</c> CLI (issue F2-05).</summary>
internal static class ExitCodes
{
    /// <summary>The command succeeded (including "nothing to change").</summary>
    public const int Success = 0;

    /// <summary>Invalid usage: no or unknown command, missing/extra arguments, invalid email or name.</summary>
    public const int InvalidUsage = 1;

    /// <summary>No administrator has the given email.</summary>
    public const int NotFound = 2;

    /// <summary>Missing/invalid configuration or a database error (not covered by the issue's codes 0–2).</summary>
    public const int Failure = 3;
}
