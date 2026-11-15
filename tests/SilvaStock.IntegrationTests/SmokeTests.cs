namespace SilvaStock.IntegrationTests;

public class SmokeTests
{
    [Fact]
    public void ApiAssemblyIsReferenced()
    {
        // Placeholder until F1-05 adds WebApplicationFactory + Testcontainers fixtures.
        Assert.Equal("SilvaStock.Api", typeof(Program).Assembly.GetName().Name);
    }
}
