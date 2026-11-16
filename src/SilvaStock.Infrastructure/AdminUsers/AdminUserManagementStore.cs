using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SilvaStock.Application.AdminUsers.Management;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.Infrastructure.Persistence;

namespace SilvaStock.Infrastructure.AdminUsers;

/// <summary>
/// EF Core implementation of <see cref="IAdminUserManagementStore"/>. The lookup runs <c>SELECT … FOR UPDATE</c> inside
/// a transaction, so the read-modify-write of <c>session_version</c> cannot race with a logout or another CLI run.
/// </summary>
internal sealed class AdminUserManagementStore(SilvaStockDbContext dbContext)
    : IAdminUserManagementStore, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;

    public async Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.AdminUsers
            .AsNoTracking()
            .OrderBy(a => a.Email)
            .ToListAsync(cancellationToken);

    public async Task<AdminUser?> FindByEmailForUpdateAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);

        _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Not composed further (no LIMIT wrapper): the unique index guarantees at most one row.
        var rows = await dbContext.AdminUsers
            .FromSql($"SELECT * FROM admin_users WHERE email = {normalizedEmail} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public void Add(AdminUser adminUser)
    {
        ArgumentNullException.ThrowIfNull(adminUser);
        dbContext.AdminUsers.Add(adminUser);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Disposing an uncommitted transaction rolls it back and releases the row lock.
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
