using System.Globalization;

// Admin management commands (add-admin, remove-admin, list-admins) are implemented in F2-05.
await Console.Error.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"Usage: silvastock <command> [options]{Environment.NewLine}No commands are available yet."));
return args.Length == 0 ? 0 : 1;
