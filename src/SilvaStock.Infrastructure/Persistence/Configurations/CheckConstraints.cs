using System.Globalization;

namespace SilvaStock.Infrastructure.Persistence.Configurations;

/// <summary>Builds CHECK constraint SQL for enum columns stored as text.</summary>
internal static class CheckConstraints
{
    /// <summary>
    /// <c>column IN ('A', 'B', ...)</c> with every member name of <typeparamref name="TEnum"/>.
    /// A NULL value passes (CHECK treats unknown as satisfied), so it also fits nullable columns.
    /// </summary>
    public static string EnumValues<TEnum>(string column)
        where TEnum : struct, Enum
    {
        var values = string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"));
        return string.Create(CultureInfo.InvariantCulture, $"{column} IN ({values})");
    }
}
