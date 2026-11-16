namespace SilvaStock.Domain.Vehicles;

// Member names are part of the API contract (openapi.yaml) and are stored as text in the database:
// never rename a member without a migration and a contract change.

/// <summary>Vehicle lifecycle status (DECISIONS section 7).</summary>
public enum VehicleStatus
{
    Draft,
    Published,
    Sold,
}

/// <summary>Fuel type.</summary>
public enum FuelType
{
    Petrol,
    Diesel,
    Hybrid,
    PlugInHybrid,
    Electric,
    Lpg,
    Other,
}

/// <summary>Gearbox type.</summary>
public enum Transmission
{
    Manual,
    Automatic,
}

/// <summary>Driven wheels.</summary>
public enum Drivetrain
{
    FWD,
    RWD,
    AWD,
}

/// <summary>Body style.</summary>
public enum BodyType
{
    Hatchback,
    Sedan,
    Wagon,
    Suv,
    Coupe,
    Convertible,
    Mpv,
    Van,
    Pickup,
    Other,
}

/// <summary>Category of a private vehicle document.</summary>
public enum DocumentCategory
{
    /// <summary>Vehicle registration certificate (DUA).</summary>
    RegistrationDocument,
    Inspection,
    Maintenance,
    Warranty,
    Purchase,
    Other,
}
