using SilvaStock.Application.AdminUsers.Management;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Cli;

/// <summary>
/// Hand-written parser for the four allowlist commands (no CLI-parsing package: the grammar is one command, one
/// email and one optional <c>--name</c>). Validates the email and name syntax here so usage errors (exit code 1)
/// are reported before any configuration is read or connection opened.
/// </summary>
internal static class CommandLineParser
{
    public const string Usage = """
        Usage: silvastock <command> [arguments]

        Commands:
          add-admin <email> [--name "Name"]   Add an administrator, or reactivate an inactive one (idempotent).
          remove-admin <email>                Deactivate an administrator and sign them out everywhere.
          reset-google <email>                Unbind the Google account and sign the administrator out everywhere.
          list-admins                         List every administrator.
          help                                Show this help.

        The connection string is read from the environment variable ConnectionStrings__Default
        or from user-secrets (ConnectionStrings:Default); it is never passed as an argument.

        Exit codes: 0 success, 1 invalid usage, 2 administrator not found, 3 configuration or database error.
        """;

    private const string _nameOption = "--name";

    public static ParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0)
        {
            return ParseResult.Fail("No command given.");
        }

        var command = args[0];
        var rest = args.Skip(1).ToList();

        return command switch
        {
            "help" or "--help" or "-h" => rest.Count == 0
                ? ParseResult.Ok(new CliCommand.Help())
                : ParseResult.Fail($"'{command}' takes no arguments."),
            "add-admin" => ParseAddAdmin(rest),
            "remove-admin" => ParseEmailOnly(command, rest, email => new CliCommand.RemoveAdmin(email)),
            "reset-google" => ParseEmailOnly(command, rest, email => new CliCommand.ResetGoogle(email)),
            "list-admins" => rest.Count == 0
                ? ParseResult.Ok(new CliCommand.ListAdmins())
                : ParseResult.Fail("'list-admins' takes no arguments."),
            _ => ParseResult.Fail($"Unknown command '{command}'."),
        };
    }

    private static ParseResult ParseAddAdmin(List<string> args)
    {
        string? email = null;
        string? name = null;
        var nameSeen = false;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string? value;

            if (arg == _nameOption)
            {
                if (i + 1 >= args.Count)
                {
                    return ParseResult.Fail("Option '--name' requires a value.");
                }

                value = args[++i];
            }
            else if (arg.StartsWith(_nameOption + "=", StringComparison.Ordinal))
            {
                value = arg[(_nameOption.Length + 1)..];
            }
            else if (arg.StartsWith('-'))
            {
                return ParseResult.Fail($"Unknown option '{arg}'.");
            }
            else
            {
                if (email is not null)
                {
                    return ParseResult.Fail("'add-admin' takes exactly one email.");
                }

                email = arg;
                continue;
            }

            if (nameSeen)
            {
                return ParseResult.Fail("Option '--name' given more than once.");
            }

            nameSeen = true;
            name = value;
        }

        if (email is null)
        {
            return ParseResult.Fail("'add-admin' requires an email.");
        }

        if (!AdminEmail.TryNormalize(email, out var normalized))
        {
            return ParseResult.Fail("Invalid email address.");
        }

        if (name is not null && !AdminDisplayName.IsValid(name))
        {
            return ParseResult.Fail(
                $"Invalid name: it must not be blank or contain control characters, and must have at most {AdminUser.DisplayNameMaxLength} characters.");
        }

        return ParseResult.Ok(new CliCommand.AddAdmin(normalized, name));
    }

    private static ParseResult ParseEmailOnly(string command, List<string> args, Func<string, CliCommand> create)
    {
        if (args.Count != 1 || args[0].StartsWith('-'))
        {
            return ParseResult.Fail($"'{command}' takes exactly one email.");
        }

        return AdminEmail.TryNormalize(args[0], out var normalized)
            ? ParseResult.Ok(create(normalized))
            : ParseResult.Fail("Invalid email address.");
    }
}
