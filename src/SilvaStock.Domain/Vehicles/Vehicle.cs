namespace SilvaStock.Domain.Vehicles;

/// <summary>
/// A vehicle in stock. Editable data mirrors <c>VehicleInput</c> in <c>openapi.yaml</c>; status transitions and
/// publish validation (DECISIONS section 7) are implemented in F3-02, so the status cannot be changed here yet.
/// <see cref="RegistrationNumber"/>, <see cref="Vin"/> and <see cref="InternalNotes"/> are private: never
/// expose them in public DTOs or queries.
/// </summary>
public sealed class Vehicle
{
    public const int MakeMaxLength = 60;
    public const int ModelMaxLength = 60;
    public const int VersionMaxLength = 120;
    public const int ColorMaxLength = 40;
    public const int EmissionsMaxLength = 40;
    public const int DescriptionMaxLength = 5000;
    public const int EquipmentMaxItems = 100;
    public const int EquipmentItemMaxLength = 100;
    public const int RegistrationNumberMaxLength = 16;
    public const int VinLength = 17;
    public const int InternalNotesMaxLength = 5000;

    /// <summary>Total digits of the price (<c>numeric(10,2)</c>).</summary>
    public const int PricePrecision = 10;

    /// <summary>Decimal places of the price.</summary>
    public const int PriceScale = 2;

    /// <summary>Creates a Draft vehicle; only make and model are required to save a Draft.</summary>
    public Vehicle(string make, string model, string createdBy, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(make);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        Make = make;
        Model = model;
        Status = VehicleStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedBy = createdBy;
        UpdatedAt = CreatedAt;
    }

    public long Id { get; private set; }

    public VehicleStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? SoldAt { get; private set; }

    /// <summary>Required; never blank.</summary>
    public string Make
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }

    /// <summary>Required; never blank.</summary>
    public string Model
    {
        get;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }

    public string? Version { get; set; }

    public int? Year { get; set; }

    public int? MileageKm { get; set; }

    /// <summary>Price in EUR; null when not set (e.g. <see cref="PriceOnRequest"/>).</summary>
    public decimal? Price { get; set; }

    public bool PriceOnRequest { get; set; }

    public FuelType? Fuel { get; set; }

    public Transmission? Transmission { get; set; }

    public int? EngineCc { get; set; }

    public int? PowerKw { get; set; }

    public BodyType? BodyType { get; set; }

    /// <summary>Exterior colour.</summary>
    public string? Color { get; set; }

    public string? InteriorColor { get; set; }

    public int? Doors { get; set; }

    public int? Seats { get; set; }

    public Drivetrain? Drivetrain { get; set; }

    /// <summary>E.g. "Euro 6d" or "120 g/km".</summary>
    public string? Emissions { get; set; }

    public string? Description { get; set; }

    /// <summary>List of extras (Portuguese text); empty when none.</summary>
    public IList<string> Equipment { get; private set; } = new List<string>();

    /// <summary>Private.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Private.</summary>
    public string? Vin { get; set; }

    /// <summary>Private.</summary>
    public string? InternalNotes { get; set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Email of the admin who created the vehicle (kept even if the admin is deactivated).</summary>
    public string CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Email of the admin who last changed the vehicle.</summary>
    public string UpdatedBy { get; private set; }

    /// <summary>Records who changed the vehicle and when.</summary>
    public void MarkUpdated(string updatedBy, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt.ToUniversalTime();
    }
}
