using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webapi.Migrations
{
    /// <inheritdoc />
    public partial class SeedApplicationData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "ApplicationId", "ApplicationName", "ClientId", "Scopes", "Secret" },
                values: new object[] { 1, "MVCWebApp", "53D3C1E6-5487-8C6E-A8E4BD59940E", "read,write,delete", "0673FC70-0514-4011-CCA3-DF9BC03201BC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Applications",
                keyColumn: "ApplicationId",
                keyValue: 1);
        }
    }
}
