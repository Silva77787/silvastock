namespace SilvaStock.Application.AdminUsers.Management;

/// <summary>
/// Thrown by <see cref="IAdminUserManagementStore.SaveChangesAsync"/> when a new administrator could not be inserted
/// because another one with the same email was inserted concurrently. The unit of work has been rolled back and can
/// be used again.
/// </summary>
public sealed class AdminEmailAlreadyExistsException : Exception
{
    public AdminEmailAlreadyExistsException()
        : base("An administrator with this email already exists.")
    {
    }

    public AdminEmailAlreadyExistsException(string message)
        : base(message)
    {
    }

    public AdminEmailAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
