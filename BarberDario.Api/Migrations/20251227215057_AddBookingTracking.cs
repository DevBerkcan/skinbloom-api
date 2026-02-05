using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BarberDario.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("13a514e0-4b48-439f-b2f4-7c83bf06c40d"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("56e2422b-b8ca-4d0f-9afc-b4ea46140673"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("dbe7723d-35cc-485f-9e6e-04e3a0639f94"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("e0b75c2e-3d58-4f0c-82bd-24db70b471bd"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("e54cf1ea-6cef-40a5-9da1-82f5509b61db"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("ef78c874-bdc8-44f3-bb38-d5566581d2c9"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("fa184a8e-ed14-4edc-91bc-7455c0c9a29f"));

            migrationBuilder.AddColumn<string>(
                name: "ReferrerUrl",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "BusinessHours",
                columns: new[] { "Id", "BreakEndTime", "BreakStartTime", "CloseTime", "DayOfWeek", "IsOpen", "OpenTime" },
                values: new object[,]
                {
                    { new Guid("43cf2ba7-97ce-401c-a44d-4a4e978dacf1"), null, null, new TimeOnly(18, 0, 0), 3, true, new TimeOnly(9, 0, 0) },
                    { new Guid("5c96e3ad-8ee6-4044-8b49-9afae24e1118"), null, null, new TimeOnly(16, 0, 0), 6, true, new TimeOnly(9, 0, 0) },
                    { new Guid("77a854e4-861c-474d-88b1-da40bb0c151b"), null, null, new TimeOnly(0, 0, 0), 0, false, new TimeOnly(0, 0, 0) },
                    { new Guid("d5396705-fde0-47c1-997f-3a410940fa70"), null, null, new TimeOnly(20, 0, 0), 4, true, new TimeOnly(9, 0, 0) },
                    { new Guid("d9f4da3e-9548-455c-9f54-9620cd9355ca"), null, null, new TimeOnly(18, 0, 0), 2, true, new TimeOnly(9, 0, 0) },
                    { new Guid("e0c4734d-0c8c-49ed-8fa3-ad254849a5e4"), null, null, new TimeOnly(18, 0, 0), 1, true, new TimeOnly(9, 0, 0) },
                    { new Guid("f2a8b7c9-e383-4d6b-aeca-f385544a0a30"), null, null, new TimeOnly(18, 0, 0), 5, true, new TimeOnly(9, 0, 0) }
                });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7300), new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7300) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7330), new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7330) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7330), new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7330) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7340), new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7340) });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "ADMIN_EMAIL",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BOOKING_INTERVAL_MINUTES",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BUSINESS_ADDRESS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7530));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BUSINESS_NAME",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7530));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "MAX_ADVANCE_BOOKING_DAYS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "MIN_ADVANCE_BOOKING_HOURS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "REMINDER_HOURS_BEFORE",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "TIMEZONE",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 21, 50, 57, 557, DateTimeKind.Utc).AddTicks(7530));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("43cf2ba7-97ce-401c-a44d-4a4e978dacf1"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("5c96e3ad-8ee6-4044-8b49-9afae24e1118"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("77a854e4-861c-474d-88b1-da40bb0c151b"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("d5396705-fde0-47c1-997f-3a410940fa70"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("d9f4da3e-9548-455c-9f54-9620cd9355ca"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("e0c4734d-0c8c-49ed-8fa3-ad254849a5e4"));

            migrationBuilder.DeleteData(
                table: "BusinessHours",
                keyColumn: "Id",
                keyValue: new Guid("f2a8b7c9-e383-4d6b-aeca-f385544a0a30"));

            migrationBuilder.DropColumn(
                name: "ReferrerUrl",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "Bookings");

            migrationBuilder.InsertData(
                table: "BusinessHours",
                columns: new[] { "Id", "BreakEndTime", "BreakStartTime", "CloseTime", "DayOfWeek", "IsOpen", "OpenTime" },
                values: new object[,]
                {
                    { new Guid("13a514e0-4b48-439f-b2f4-7c83bf06c40d"), null, null, new TimeOnly(20, 0, 0), 4, true, new TimeOnly(9, 0, 0) },
                    { new Guid("56e2422b-b8ca-4d0f-9afc-b4ea46140673"), null, null, new TimeOnly(18, 0, 0), 2, true, new TimeOnly(9, 0, 0) },
                    { new Guid("dbe7723d-35cc-485f-9e6e-04e3a0639f94"), null, null, new TimeOnly(0, 0, 0), 0, false, new TimeOnly(0, 0, 0) },
                    { new Guid("e0b75c2e-3d58-4f0c-82bd-24db70b471bd"), null, null, new TimeOnly(18, 0, 0), 1, true, new TimeOnly(9, 0, 0) },
                    { new Guid("e54cf1ea-6cef-40a5-9da1-82f5509b61db"), null, null, new TimeOnly(16, 0, 0), 6, true, new TimeOnly(9, 0, 0) },
                    { new Guid("ef78c874-bdc8-44f3-bb38-d5566581d2c9"), null, null, new TimeOnly(18, 0, 0), 3, true, new TimeOnly(9, 0, 0) },
                    { new Guid("fa184a8e-ed14-4edc-91bc-7455c0c9a29f"), null, null, new TimeOnly(18, 0, 0), 5, true, new TimeOnly(9, 0, 0) }
                });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8130), new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8130) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8150), new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8150) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8160), new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8160) });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8160), new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8160) });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "ADMIN_EMAIL",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BOOKING_INTERVAL_MINUTES",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BUSINESS_ADDRESS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "BUSINESS_NAME",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "MAX_ADVANCE_BOOKING_DAYS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "MIN_ADVANCE_BOOKING_HOURS",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "REMINDER_HOURS_BEFORE",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Key",
                keyValue: "TIMEZONE",
                column: "UpdatedAt",
                value: new DateTime(2025, 12, 27, 20, 27, 53, 687, DateTimeKind.Utc).AddTicks(8320));
        }
    }
}
