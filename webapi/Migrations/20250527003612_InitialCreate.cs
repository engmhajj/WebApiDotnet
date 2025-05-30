using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace webapi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Shirts",
                keyColumn: "ShirtId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Shirts",
                keyColumn: "ShirtId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Shirts",
                keyColumn: "ShirtId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Shirts",
                keyColumn: "ShirtId",
                keyValue: 4);

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationName = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<string>(type: "TEXT", nullable: false),
                    Scopes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefreshTokenId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedFromIp = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceInfo = table.Column<string>(type: "TEXT", nullable: true),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.RefreshTokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ClientId",
                table: "Applications",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.InsertData(
                table: "Shirts",
                columns: new[] { "ShirtId", "Brand", "Color", "Gender", "Price", "Size" },
                values: new object[,]
                {
                    { 1, "hamada", "Blue", "Men", 30.0, 10 },
                    { 2, "My brand", "Black", "Men", 35.0, 12 },
                    { 3, "your brand", "Pink", "Women", 28.0, 8 },
                    { 4, "your brand", "yello", "Women", 30.0, 9 }
                });
        }
    }
}
