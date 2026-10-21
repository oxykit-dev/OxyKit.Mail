using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OxyKit.Mail.Infrastructure.Sqlite.Migrations;

[DbContext(typeof(OxyKit.Mail.Infrastructure.ApplicationDbContext))]
[Migration("20261008120000_RemoveEndpointPermissions")]
public partial class RemoveEndpointPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EndpointPermissions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EndpointPermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Route = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                AllowedRoles = table.Column<string>(type: "TEXT", nullable: false),
                AllowedPermissions = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EndpointPermissions", x => x.Id);
            }
        );
    }
}
