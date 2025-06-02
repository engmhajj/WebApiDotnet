using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webapi.Migrations;

/// <inheritdoc />
public partial class update2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Applications",
            columns: table => new
            {
                ApplicationId = table
                    .Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ApplicationName = table.Column<string>(type: "TEXT", nullable: false),
                ClientId = table.Column<string>(type: "TEXT", nullable: false),
                SecretSalt = table.Column<string>(type: "TEXT", nullable: false),
                SecretHash = table.Column<string>(type: "TEXT", nullable: false),
                Scopes = table.Column<string>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Applications", x => x.ApplicationId);
            }
        );

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                RefreshTokenId = table
                    .Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Token = table.Column<string>(type: "TEXT", nullable: false),
                ClientId = table.Column<string>(type: "TEXT", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedFromIp = table.Column<string>(type: "TEXT", nullable: true),
                DeviceInfo = table.Column<string>(type: "TEXT", nullable: true),
                IsRevoked = table.Column<bool>(
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: false
                ),
                RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                RevokedByIp = table.Column<string>(type: "TEXT", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.RefreshTokenId);
            }
        );

        migrationBuilder.CreateTable(
            name: "Shirts",
            columns: table => new
            {
                ShirtId = table
                    .Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Brand = table.Column<string>(type: "TEXT", nullable: false),
                Color = table.Column<string>(type: "TEXT", nullable: false),
                Size = table.Column<int>(type: "INTEGER", nullable: true),
                Gender = table.Column<string>(type: "TEXT", nullable: false),
                Price = table.Column<double>(type: "REAL", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Shirts", x => x.ShirtId);
            }
        );

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                UserId = table
                    .Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                Roles = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.UserId);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_Applications_ClientId",
            table: "Applications",
            column: "ClientId",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Applications");

        migrationBuilder.DropTable(name: "RefreshTokens");

        migrationBuilder.DropTable(name: "Shirts");

        migrationBuilder.DropTable(name: "Users");
    }
}
