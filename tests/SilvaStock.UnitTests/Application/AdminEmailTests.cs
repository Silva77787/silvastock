using SilvaStock.Application.AdminUsers.Management;

namespace SilvaStock.UnitTests.Application;

public class AdminEmailTests
{
    [Theory]
    [InlineData("admin@example.test", "admin@example.test")]
    [InlineData("Admin@Example.TEST", "admin@example.test")]
    [InlineData("  maria.silva+stock@sub.example.test  ", "maria.silva+stock@sub.example.test")]
    public void ValidEmailsAreTrimmedAndLowerCased(string input, string expected)
    {
        Assert.True(AdminEmail.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("admin")]
    [InlineData("admin@")]
    [InlineData("@example.test")]
    [InlineData("admin@localhost")]
    [InlineData("admin@@example.test")]
    [InlineData("a@b@example.test")]
    [InlineData("admin @example.test")]
    [InlineData("admin@exa mple.test")]
    [InlineData("Admin <admin@example.test>")]
    [InlineData("admin@example..test")]
    [InlineData("admin@.example.test")]
    [InlineData("admin@example.test.")]
    [InlineData("admin\t@example.test")]
    public void InvalidEmailsAreRejected(string? input)
    {
        Assert.False(AdminEmail.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void EmailsLongerThanTheMaximumAreRejected()
    {
        var tooLong = new string('a', 64) + "@" + new string('b', 186) + ".test"; // 255 characters

        Assert.False(AdminEmail.TryNormalize(tooLong, out _));
    }

    [Theory]
    [InlineData("Maria Silva", true)]
    [InlineData("  Maria  ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Maria\nSilva", false)]
    public void DisplayNameValidation(string displayName, bool expected)
    {
        Assert.Equal(expected, AdminDisplayName.IsValid(displayName));
    }

    [Fact]
    public void DisplayNameAtTheMaximumLengthIsValidAndOneMoreIsNot()
    {
        Assert.True(AdminDisplayName.IsValid(new string('n', 100)));
        Assert.True(AdminDisplayName.IsValid("  " + new string('n', 100) + "  "));
        Assert.False(AdminDisplayName.IsValid(new string('n', 101)));
    }
}
