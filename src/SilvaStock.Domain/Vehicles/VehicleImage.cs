namespace SilvaStock.Domain.Vehicles;

/// <summary>
/// A photo of a vehicle, stored in the public bucket under a random key (DECISIONS section 8).
/// At most one image per vehicle is primary (enforced by a partial unique index). Upload, ordering and
/// primary selection rules are implemented in Phase 4.
/// </summary>
public sealed class VehicleImage
{
    public const int ObjectKeyMaxLength = 512;
    public const int ContentTypeMaxLength = 100;

    public VehicleImage(
        long vehicleId,
        string objectKey,
        string thumbKey,
        string contentType,
        long sizeBytes,
        int sortOrder,
        bool isPrimary,
        int? width = null,
        int? height = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        if (width is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width.Value, nameof(width));
        }

        if (height is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height.Value, nameof(height));
        }

        VehicleId = vehicleId;
        ObjectKey = objectKey;
        ThumbKey = thumbKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        Width = width;
        Height = height;
    }

    public long Id { get; private set; }

    public long VehicleId { get; private set; }

    /// <summary>Storage key of the full-size (re-encoded) image.</summary>
    public string ObjectKey { get; private set; }

    /// <summary>Storage key of the WebP thumbnail.</summary>
    public string ThumbKey { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    /// <summary>Width in pixels of the re-encoded image; null when unknown.</summary>
    public int? Width { get; private set; }

    /// <summary>Height in pixels of the re-encoded image; null when unknown.</summary>
    public int? Height { get; private set; }
}
