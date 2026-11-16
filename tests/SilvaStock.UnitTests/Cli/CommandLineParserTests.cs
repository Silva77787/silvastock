using SilvaStock.Cli;

namespace SilvaStock.UnitTests.Cli;

public class CommandLineParserTests
{
    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void HelpIsRecognised(string arg)
    {
        Assert.IsType<CliCommand.Help>(CommandLineParser.Parse([arg]).Command);
    }

    [Fact]
    public void AddAdminWithEmailOnly()
    {
        var command = CommandLineParser.Parse(["add-admin", "Maria@Example.TEST"]).Command;

        Assert.Equal(new CliCommand.AddAdmin("maria@example.test", null), command);
    }

    [Theory]
    [InlineData("add-admin", "maria@example.test", "--name", "Maria Silva")]
    [InlineData("add-admin", "--name", "Maria Silva", "maria@example.test")]
    public void AddAdminWithNameInAnyPosition(params string[] args)
    {
        var command = CommandLineParser.Parse(args).Command;

        Assert.Equal(new CliCommand.AddAdmin("maria@example.test", "Maria Silva"), command);
    }

    [Fact]
    public void AddAdminAcceptsNameWithEqualsSign()
    {
        var command = CommandLineParser.Parse(["add-admin", "maria@example.test", "--name=Maria Silva"]).Command;

        Assert.Equal(new CliCommand.AddAdmin("maria@example.test", "Maria Silva"), command);
    }

    [Fact]
    public void RemoveAdminResetGoogleAndListAdmins()
    {
        Assert.Equal(
            new CliCommand.RemoveAdmin("maria@example.test"),
            CommandLineParser.Parse(["remove-admin", "MARIA@example.test"]).Command);
        Assert.Equal(
            new CliCommand.ResetGoogle("maria@example.test"),
            CommandLineParser.Parse(["reset-google", "maria@EXAMPLE.test"]).Command);
        Assert.IsType<CliCommand.ListAdmins>(CommandLineParser.Parse(["list-admins"]).Command);
    }

    [Theory]
    [InlineData]
    [InlineData("unknown")]
    [InlineData("ADD-ADMIN", "maria@example.test")]
    [InlineData("help", "extra")]
    [InlineData("add-admin")]
    [InlineData("add-admin", "not-an-email")]
    [InlineData("add-admin", "maria@example.test", "other@example.test")]
    [InlineData("add-admin", "maria@example.test", "--name")]
    [InlineData("add-admin", "maria@example.test", "--name", "   ")]
    [InlineData("add-admin", "maria@example.test", "--name=")]
    [InlineData("add-admin", "maria@example.test", "--name", "A", "--name", "B")]
    [InlineData("add-admin", "maria@example.test", "--connection", "Host=x")]
    [InlineData("remove-admin")]
    [InlineData("remove-admin", "not-an-email")]
    [InlineData("remove-admin", "maria@example.test", "extra@example.test")]
    [InlineData("remove-admin", "--force")]
    [InlineData("reset-google")]
    [InlineData("reset-google", "maria@")]
    [InlineData("list-admins", "extra")]
    public void InvalidUsageIsAnError(params string[] args)
    {
        var result = CommandLineParser.Parse(args);

        Assert.Null(result.Command);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void NameLongerThanTheMaximumIsAnError()
    {
        var result = CommandLineParser.Parse(["add-admin", "maria@example.test", "--name", new string('n', 101)]);

        Assert.Null(result.Command);
    }
}
