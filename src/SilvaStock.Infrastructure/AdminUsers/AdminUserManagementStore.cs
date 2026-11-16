using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
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
    /// <summary>Unique index on <c>admin_users.email</c> (InitialCreate migration).</summary>
    private const string _emailUniqueIndex = "ix_admin_users_email";

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
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: _emailUniqueIndex,
        })
        {
            // The failed statement aborted the transaction: roll back and forget the staged insert so the
            // caller can look the row up again in a fresh unit of work.
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            dbContext.ChangeTracker.Clear();
            throw new AdminEmailAlreadyExistsException("An administrator with this email already exists.", ex);
        }

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
