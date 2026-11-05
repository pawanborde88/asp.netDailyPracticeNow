using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dailypracticeASPDotNet.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHardcodedSeededUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "CreatedAt", "EmailId", "FirstName", "LastName", "MiddleName", "MobileNo", "PasswordHash", "RoleId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "HQ Office, Mumbai, India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", "System", "Admin", "Super", "9876543210", "$2a$11$OsPalY7ze9VsBPps3J55Zu5sTbv3VjvgRxWSWLS7ktZ1J5M4WZXDe", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Pune, Maharashtra, India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user@example.com", "Pawan", "Borde", "", "9123456780", "$2a$11$OsPalY7ze9VsBPps3J55Zu5sTbv3VjvgRxWSWLS7ktZ1J5M4WZXDe", 2 }
                });
        }
    }
}
