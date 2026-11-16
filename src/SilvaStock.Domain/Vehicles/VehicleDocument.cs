namespace SilvaStock.Domain.Vehicles;

/// <summary>
/// A private document of a vehicle, stored in the private bucket only and never exposed publicly
/// (DECISIONS section 8). Upload and download rules are implemented in Phase 4.
/// </summary>
public sealed class VehicleDocument
{
    public const int DisplayNameMaxLength = 200;
    public const int ObjectKeyMaxLength = 512;
    public const int ContentTypeMaxLength = 100;

    public VehicleDocument(
        long vehicleId,
        DocumentCategory category,
        string displayName,
        string objectKey,
        string contentType,
        long sizeBytes,
        DateTimeOffset uploadedAt,
        string uploadedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedBy);

        VehicleId = vehicleId;
        Category = category;
        DisplayName = displayName;
        ObjectKey = objectKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = uploadedAt.ToUniversalTime();
        UploadedBy = uploadedBy;
    }

    public long Id { get; private set; }

    public long VehicleId { get; private set; }

    public DocumentCategory Category { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>Storage key in the private bucket.</summary>
    public string ObjectKey { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>Email of the admin who uploaded the document.</summary>
    public string UploadedBy { get; private set; }
}
