using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SilvaStock.Domain.AdminUsers;
using SilvaStock.Domain.Vehicles;
using SilvaStock.Infrastructure.Persistence;
using SilvaStock.IntegrationTests.Infrastructure;

namespace SilvaStock.IntegrationTests.Persistence;

/// <summary>Database constraints of the MVP schema (DECISIONS section 6), against the real migrations.</summary>
[Collection(IntegrationTestGroup.Name)]
public sealed class SchemaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string _admin = "admin@example.test";

    private static readonly DateTimeOffset _now = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    private static readonly string[] _tables =
        ["admin_users", "vehicles", "vehicle_images", "vehicle_documents", "audit_log", "data_protection_keys"];

    public Task InitializeAsync() => MigratedDatabase.PrepareAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrationsExistAndMatchTheModel()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        Assert.NotEmpty(context.Database.GetMigrations());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges(), "The model has changes not captured in a migration.");
    }

    [Fact]
    public async Task MigrationsCreateEveryTable()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        Assert.Equal(_tables.Order(StringComparer.Ordinal), await ExistingTablesAsync(context));
    }

    [Fact]
    public async Task MigrationsAreReversible()
    {
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(Migration.InitialDatabase);
            Assert.Empty(await ExistingTablesAsync(context));

            await migrator.MigrateAsync();
            Assert.Equal(_tables.Order(StringComparer.Ordinal), await ExistingTablesAsync(context));
        }
    }

    [Fact]
    public async Task ApiRegistersTheContextOnTheConfiguredDatabase()
    {
        await using var factory = postgres.CreateFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SilvaStockDbContext>();

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Equal(0, await context.AdminUsers.CountAsync());
    }

    [Theory]
    [InlineData("Reserved")]
    [InlineData("draft")]
    public async Task StatusCheckRejectsUnknownValues(string status)
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync($"""
            INSERT INTO vehicles (status, make, model, created_at, created_by, updated_at, updated_by)
            VALUES ({status}, 'BMW', '320d', {_now}, {_admin}, {_now}, {_admin})
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_vehicles_status", ex.ConstraintName);
    }

    [Fact]
    public async Task StatusIsStoredAsTheEnumName()
    {
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            context.Vehicles.Add(NewVehicle());
            await context.SaveChangesAsync();
        }

        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var statuses = await context.Database
                .SqlQuery<string>($"SELECT status AS \"Value\" FROM vehicles")
                .ToListAsync();

            Assert.Equal(["Draft"], statuses);
        }
    }

    [Fact]
    public async Task EmailIsUniqueRegardlessOfCase()
    {
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            context.AdminUsers.Add(new AdminUser("Admin@example.test", displayName: null, _now));
            await context.SaveChangesAsync();
        }

        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            context.AdminUsers.Add(new AdminUser("admin@example.test", displayName: null, _now));

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            AssertPostgresError(ex, PostgresErrorCodes.UniqueViolation, "ix_admin_users_email");
        }
    }

    [Fact]
    public async Task EmailMustBeStoredInLowerCase()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        // Bypasses the domain normalisation: the database still refuses a mixed-case email.
        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync($"""
            INSERT INTO admin_users (email, created_at) VALUES ('Admin@example.test', {_now})
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_admin_users_email_lower", ex.ConstraintName);
    }

    [Fact]
    public async Task AdminUserDefaultsApplyToRowsInsertedWithSql()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO admin_users (email, created_at) VALUES ('admin@example.test', {_now})
            """);

        var user = await context.AdminUsers.SingleAsync();
        Assert.True(user.IsActive);
        Assert.Equal(0, user.SessionVersion);
        Assert.Null(user.GoogleSub);
        Assert.Null(user.LastLoginAt);
        Assert.Null(user.DisplayName);
    }

    [Fact]
    public async Task GoogleSubIsUniqueButMayBeNullForManyAdmins()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        context.AdminUsers.AddRange(
            new AdminUser("one@example.test", displayName: null, _now),
            new AdminUser("two@example.test", displayName: null, _now),
            new AdminUser("three@example.test", displayName: null, _now));
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlAsync(
            $"UPDATE admin_users SET google_sub = 'google-sub-test-1' WHERE email = 'one@example.test'");

        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"UPDATE admin_users SET google_sub = 'google-sub-test-1' WHERE email = 'two@example.test'"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        Assert.Equal("ix_admin_users_google_sub", ex.ConstraintName);
    }

    [Fact]
    public async Task OnlyOnePrimaryImagePerVehicle()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var first = NewVehicle();
        var second = NewVehicle();
        context.Vehicles.AddRange(first, second);
        await context.SaveChangesAsync();

        // One primary per vehicle and any number of non-primary images are allowed.
        context.VehicleImages.AddRange(
            NewImage(first.Id, sortOrder: 0, isPrimary: true),
            NewImage(first.Id, sortOrder: 1, isPrimary: false),
            NewImage(first.Id, sortOrder: 2, isPrimary: false),
            NewImage(second.Id, sortOrder: 0, isPrimary: true));
        await context.SaveChangesAsync();

        context.VehicleImages.Add(NewImage(first.Id, sortOrder: 3, isPrimary: true));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        AssertPostgresError(ex, PostgresErrorCodes.UniqueViolation, "ix_vehicle_images_vehicle_id_primary");
    }

    [Fact]
    public async Task ImageDimensionsRoundTripAndMayBeNull()
    {
        long vehicleId;
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var vehicle = NewVehicle();
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
            vehicleId = vehicle.Id;

            context.VehicleImages.AddRange(
                new VehicleImage(vehicleId, "vehicles/a.webp", "vehicles/a-thumb.webp", "image/webp", 2048, 0, true, width: 1600, height: 1200),
                NewImage(vehicleId, sortOrder: 1, isPrimary: false));
            await context.SaveChangesAsync();
        }

        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var images = await context.VehicleImages
                .Where(i => i.VehicleId == vehicleId)
                .OrderBy(i => i.SortOrder)
                .ToListAsync();

            Assert.Equal(1600, images[0].Width);
            Assert.Equal(1200, images[0].Height);
            Assert.Null(images[1].Width);
            Assert.Null(images[1].Height);
        }
    }

    [Theory]
    [InlineData("width", "ck_vehicle_images_width")]
    [InlineData("height", "ck_vehicle_images_height")]
    public async Task ImageDimensionsMustBePositive(string column, string constraint)
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var vehicle = NewVehicle();
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        context.VehicleImages.Add(NewImage(vehicle.Id, sortOrder: 0, isPrimary: true));
        await context.SaveChangesAsync();

        // Column names come from InlineData constants, never from input.
#pragma warning disable EF1002
        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
            $"UPDATE vehicle_images SET {column} = 0"));
#pragma warning restore EF1002

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal(constraint, ex.ConstraintName);
    }

    [Fact]
    public async Task VehicleDataRoundTrips()
    {
        long id;
        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var vehicle = NewVehicle();
            vehicle.Price = 18950.99m;
            vehicle.Fuel = FuelType.PlugInHybrid;
            vehicle.Transmission = Transmission.Automatic;
            vehicle.BodyType = BodyType.Wagon;
            vehicle.Drivetrain = Drivetrain.AWD;
            vehicle.Equipment.Add("Ar condicionado automático");
            vehicle.Equipment.Add("Sensores de estacionamento");
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
            id = vehicle.Id;
        }

        await using (var context = MigratedDatabase.CreateContext(postgres))
        {
            var stored = await context.Vehicles.SingleAsync(v => v.Id == id);

            Assert.Equal(18950.99m, stored.Price);
            Assert.Equal(FuelType.PlugInHybrid, stored.Fuel);
            Assert.Equal(Transmission.Automatic, stored.Transmission);
            Assert.Equal(BodyType.Wagon, stored.BodyType);
            Assert.Equal(Drivetrain.AWD, stored.Drivetrain);
            Assert.Equal(["Ar condicionado automático", "Sensores de estacionamento"], stored.Equipment);
            Assert.Equal(_now, stored.CreatedAt);
        }
    }

    [Fact]
    public async Task EquipmentDefaultsToAnEmptyArray()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO vehicles (status, make, model, created_at, created_by, updated_at, updated_by)
            VALUES ('Draft', 'BMW', '320d', {_now}, {_admin}, {_now}, {_admin})
            """);

        var stored = await context.Vehicles.SingleAsync();
        Assert.NotNull(stored.Equipment);
        Assert.Empty(stored.Equipment);
    }

    [Fact]
    public async Task PriceIsLimitedToTenDigitsWithTwoDecimals()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var vehicle = NewVehicle();
        vehicle.Price = 100_000_000m; // 9 integer digits: above numeric(10,2).
        context.Vehicles.Add(vehicle);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        AssertPostgresError(ex, PostgresErrorCodes.NumericValueOutOfRange, constraint: null);
    }

    [Fact]
    public async Task DeletingAVehicleCascadesToItsImagesAndDocuments()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var deleted = NewVehicle();
        var kept = NewVehicle();
        context.Vehicles.AddRange(deleted, kept);
        await context.SaveChangesAsync();

        context.VehicleImages.AddRange(
            NewImage(deleted.Id, sortOrder: 0, isPrimary: true),
            NewImage(deleted.Id, sortOrder: 1, isPrimary: false),
            NewImage(kept.Id, sortOrder: 0, isPrimary: true));
        context.VehicleDocuments.AddRange(NewDocument(deleted.Id), NewDocument(kept.Id));
        await context.SaveChangesAsync();

        // Deleted in SQL (children not tracked), so the database cascade is what removes them.
        await context.Vehicles.Where(v => v.Id == deleted.Id).ExecuteDeleteAsync();

        Assert.Equal(0, await context.VehicleImages.CountAsync(i => i.VehicleId == deleted.Id));
        Assert.Equal(0, await context.VehicleDocuments.CountAsync(d => d.VehicleId == deleted.Id));
        Assert.Equal(1, await context.VehicleImages.CountAsync(i => i.VehicleId == kept.Id));
        Assert.Equal(1, await context.VehicleDocuments.CountAsync(d => d.VehicleId == kept.Id));
    }

    [Fact]
    public async Task DocumentCategoryCheckRejectsUnknownValues()
    {
        await using var context = MigratedDatabase.CreateContext(postgres);
        var vehicle = NewVehicle();
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync($"""
            INSERT INTO vehicle_documents
                (vehicle_id, category, display_name, object_key, content_type, size_bytes, uploaded_at, uploaded_by)
            VALUES ({vehicle.Id}, 'Invoice', 'Fatura', 'documents/test-key', 'application/pdf', 1024, {_now}, {_admin})
            """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal("ck_vehicle_documents_category", ex.ConstraintName);
    }

    private static Vehicle NewVehicle() => new("BMW", "320d", _admin, _now);

    private static VehicleImage NewImage(long vehicleId, int sortOrder, bool isPrimary) => new(
        vehicleId,
        objectKey: $"vehicles/{vehicleId}/{Guid.NewGuid():N}.webp",
        thumbKey: $"vehicles/{vehicleId}/{Guid.NewGuid():N}-thumb.webp",
        contentType: "image/webp",
        sizeBytes: 2048,
        sortOrder,
        isPrimary);

    private static VehicleDocument NewDocument(long vehicleId) => new(
        vehicleId,
        DocumentCategory.Inspection,
        displayName: "Inspeção 2026",
        objectKey: $"vehicles/{vehicleId}/{Guid.NewGuid():N}.pdf",
        contentType: "application/pdf",
        sizeBytes: 4096,
        uploadedAt: _now,
        uploadedBy: _admin);

    private static void AssertPostgresError(DbUpdateException ex, string sqlState, string? constraint)
    {
        var postgresError = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(sqlState, postgresError.SqlState);
        if (constraint is not null)
        {
            Assert.Equal(constraint, postgresError.ConstraintName);
        }
    }

    private static async Task<List<string>> ExistingTablesAsync(SilvaStockDbContext context)
    {
        var tables = await context.Database.SqlQuery<string>($"""
            SELECT table_name AS "Value"
              FROM information_schema.tables
             WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
            """).ToListAsync();

        return [.. tables.Order(StringComparer.Ordinal)];
    }
}
