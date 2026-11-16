using SilvaStock.Domain.Vehicles;

namespace SilvaStock.Domain.Audit;

/// <summary>An immutable record of an administrative action (e.g. a vehicle status change).</summary>
public sealed class AuditLogEntry
{
    public const int ActorMaxLength = 254;
    public const int EntityMaxLength = 50;
    public const int ActionMaxLength = 50;

    public AuditLogEntry(
        DateTimeOffset at,
        string actor,
        string entity,
        long entityId,
        string action,
        VehicleStatus? fromStatus,
        VehicleStatus? toStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        At = at.ToUniversalTime();
        Actor = actor;
        Entity = entity;
        EntityId = entityId;
        Action = action;
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }

    public long Id { get; private set; }

    public DateTimeOffset At { get; private set; }

    /// <summary>Email of the admin who performed the action (kept even if the admin is deactivated).</summary>
    public string Actor { get; private set; }

    /// <summary>Entity type name, e.g. <c>Vehicle</c>.</summary>
    public string Entity { get; private set; }

    public long EntityId { get; private set; }

    /// <summary>Action name, e.g. <c>Publish</c>.</summary>
    public string Action { get; private set; }

    public VehicleStatus? FromStatus { get; private set; }

    public VehicleStatus? ToStatus { get; private set; }
}
