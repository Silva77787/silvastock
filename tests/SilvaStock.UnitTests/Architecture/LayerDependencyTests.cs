using SilvaStock.Application;
using SilvaStock.Domain;

namespace SilvaStock.UnitTests.Architecture;

public class LayerDependencyTests
{
    [Fact]
    public void DomainReferencesOnlyTheBaseClassLibrary()
    {
        var nonFramework = typeof(DomainAssembly).Assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name ?? string.Empty)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                && !name.Equals("netstandard", StringComparison.Ordinal)
                && !name.Equals("mscorlib", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(nonFramework);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var names = typeof(ApplicationAssembly).Assembly
            .GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToList();

        Assert.DoesNotContain("SilvaStock.Infrastructure", names);
        Assert.DoesNotContain("SilvaStock.Api", names);
    }
}
