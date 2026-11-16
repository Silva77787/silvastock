using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SilvaStock.Application.AdminUsers.Management;

namespace SilvaStock.Infrastructure.AdminUsers;

/// <summary>Composition of the allowlist management services (used by the CLI).</summary>
public static class AdminUserManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AdminUserManagementService"/> and its EF Core store. Requires
    /// <see cref="DependencyInjection.AddInfrastructure"/> (database context) to be registered too.
    /// </summary>
    public static IServiceCollection AddAdminUserManagement(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAdminUserManagementStore, AdminUserManagementStore>();
        services.AddScoped<AdminUserManagementService>();
        return services;
    }
}
