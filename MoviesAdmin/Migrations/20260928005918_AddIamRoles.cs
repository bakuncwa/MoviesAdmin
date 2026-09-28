using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MoviesAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddIamRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "9a8e6b1a-9e3b-4b3a-8a2e-000000000001", "9a8e6b1a-9e3b-4b3a-8a2e-100000000001", "Admin", "ADMIN" },
                    { "9a8e6b1a-9e3b-4b3a-8a2e-000000000002", "9a8e6b1a-9e3b-4b3a-8a2e-100000000002", "Viewer", "VIEWER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9a8e6b1a-9e3b-4b3a-8a2e-000000000001");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9a8e6b1a-9e3b-4b3a-8a2e-000000000002");
        }
    }
}
