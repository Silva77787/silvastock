using Microsoft.EntityFrameworkCore;
using SilvaStock.Application.Auth;
using SilvaStock.Domain.AdminUsers;

namespace SilvaStock.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IAdminLoginStore"/>.</summary>
internal sealed class AdminLoginStore(SilvaStockDbContext dbContext) : IAdminLoginStore
{
    public Task<AdminUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => dbContext.AdminUsers.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<bool> IsGoogleAccountBoundToAnotherAdminAsync(string googleSub, long adminId, CancellationToken cancellationToken)
        => dbContext.AdminUsers.AnyAsync(u => u.GoogleSub == googleSub && u.Id != adminId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
