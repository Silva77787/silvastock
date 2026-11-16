using SilvaStock.Domain.Vehicles;

namespace SilvaStock.UnitTests.Domain;

public class VehicleTests
{
    private const string _admin = "admin@example.test";

    private static readonly DateTimeOffset _createdAt = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewVehicleIsDraftWithAuditFieldsAndNoEquipment()
    {
        var vehicle = new Vehicle("BMW", "320d", _admin, _createdAt);

        Assert.Equal(VehicleStatus.Draft, vehicle.Status);
        Assert.Null(vehicle.PublishedAt);
        Assert.Null(vehicle.SoldAt);
        Assert.False(vehicle.PriceOnRequest);
        Assert.Empty(vehicle.Equipment);
        Assert.Equal(_admin, vehicle.CreatedBy);
        Assert.Equal(_admin, vehicle.UpdatedBy);
        Assert.Equal(_createdAt, vehicle.CreatedAt);
        Assert.Equal(_createdAt, vehicle.UpdatedAt);
    }

    [Theory]
    [InlineData("", "320d")]
    [InlineData("BMW", " ")]
    public void MakeAndModelAreRequired(string make, string model)
    {
        Assert.Throws<ArgumentException>(() => new Vehicle(make, model, _admin, _createdAt));
    }

    [Fact]
    public void MarkUpdatedRecordsActorAndUtcTime()
    {
        var vehicle = new Vehicle("BMW", "320d", _admin, _createdAt);
        var later = new DateTimeOffset(2026, 11, 17, 9, 30, 0, TimeSpan.FromHours(1));

        vehicle.MarkUpdated("other@example.test", later);

        Assert.Equal("other@example.test", vehicle.UpdatedBy);
        Assert.Equal(TimeSpan.Zero, vehicle.UpdatedAt.Offset);
        Assert.Equal(later.UtcDateTime, vehicle.UpdatedAt.UtcDateTime);
        Assert.Equal(_admin, vehicle.CreatedBy);
    }

    [Fact]
    public void EnumMemberNamesMatchTheApiContract()
    {
        // Stored as text and serialised as-is: must match the enums in docs/api/openapi.yaml.
        Assert.Equal(["Draft", "Published", "Sold"], Enum.GetNames<VehicleStatus>());
        Assert.Equal(["Petrol", "Diesel", "Hybrid", "PlugInHybrid", "Electric", "Lpg", "Other"], Enum.GetNames<FuelType>());
        Assert.Equal(["Manual", "Automatic"], Enum.GetNames<Transmission>());
        Assert.Equal(["FWD", "RWD", "AWD"], Enum.GetNames<Drivetrain>());
        Assert.Equal(
            ["Hatchback", "Sedan", "Wagon", "Suv", "Coupe", "Convertible", "Mpv", "Van", "Pickup", "Other"],
            Enum.GetNames<BodyType>());
        Assert.Equal(
            ["RegistrationDocument", "Inspection", "Maintenance", "Warranty", "Purchase", "Other"],
            Enum.GetNames<DocumentCategory>());
    }
}
