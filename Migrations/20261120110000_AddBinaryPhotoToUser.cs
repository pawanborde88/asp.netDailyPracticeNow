using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dailypracticeASPDotNet.Migrations
{
    /// <inheritdoc />
    public partial class AddBinaryPhotoToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Users");
        }
    }
}
