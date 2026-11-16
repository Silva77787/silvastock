using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SilvaStock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    session_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    google_sub = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                    table.CheckConstraint("ck_admin_users_email_lower", "email = lower(email)");
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    entity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_status = table.Column<string>(type: "text", nullable: true),
                    to_status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.CheckConstraint("ck_audit_log_from_status", "from_status IN ('Draft', 'Published', 'Sold')");
                    table.CheckConstraint("ck_audit_log_to_status", "to_status IN ('Draft', 'Published', 'Sold')");
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sold_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    make = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    mileage_km = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    price_on_request = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fuel = table.Column<string>(type: "text", nullable: true),
                    transmission = table.Column<string>(type: "text", nullable: true),
                    engine_cc = table.Column<int>(type: "integer", nullable: true),
                    power_kw = table.Column<int>(type: "integer", nullable: true),
                    body_type = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    interior_color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    doors = table.Column<int>(type: "integer", nullable: true),
                    seats = table.Column<int>(type: "integer", nullable: true),
                    drivetrain = table.Column<string>(type: "text", nullable: true),
                    emissions = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    equipment = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    registration_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    vin = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_body_type", "body_type IN ('Hatchback', 'Sedan', 'Wagon', 'Suv', 'Coupe', 'Convertible', 'Mpv', 'Van', 'Pickup', 'Other')");
                    table.CheckConstraint("ck_vehicles_drivetrain", "drivetrain IN ('FWD', 'RWD', 'AWD')");
                    table.CheckConstraint("ck_vehicles_fuel", "fuel IN ('Petrol', 'Diesel', 'Hybrid', 'PlugInHybrid', 'Electric', 'Lpg', 'Other')");
                    table.CheckConstraint("ck_vehicles_status", "status IN ('Draft', 'Published', 'Sold')");
                    table.CheckConstraint("ck_vehicles_transmission", "transmission IN ('Manual', 'Automatic')");
                });

            migrationBuilder.CreateTable(
                name: "vehicle_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_documents", x => x.id);
                    table.CheckConstraint("ck_vehicle_documents_category", "category IN ('RegistrationDocument', 'Inspection', 'Maintenance', 'Warranty', 'Purchase', 'Other')");
                    table.ForeignKey(
                        name: "fk_vehicle_documents_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_images",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    thumb_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_images", x => x.id);
                    table.CheckConstraint("ck_vehicle_images_height", "height > 0");
                    table.CheckConstraint("ck_vehicle_images_width", "width > 0");
                    table.ForeignKey(
                        name: "fk_vehicle_images_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_email",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_google_sub",
                table: "admin_users",
                column: "google_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_entity_id_at",
                table: "audit_log",
                columns: new[] { "entity", "entity_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_documents_vehicle_id",
                table: "vehicle_documents",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_images_vehicle_id_primary",
                table: "vehicle_images",
                column: "vehicle_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_images_vehicle_id_sort_order",
                table: "vehicle_images",
                columns: new[] { "vehicle_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_make_model",
                table: "vehicles",
                columns: new[] { "make", "model" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_price",
                table: "vehicles",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_status_published_at",
                table: "vehicles",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_year",
                table: "vehicles",
                column: "year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "vehicle_documents");

            migrationBuilder.DropTable(
                name: "vehicle_images");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
