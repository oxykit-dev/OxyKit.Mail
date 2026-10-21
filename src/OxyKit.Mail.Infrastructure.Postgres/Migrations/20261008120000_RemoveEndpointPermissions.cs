using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OxyKit.Mail.Infrastructure.Postgres.Migrations;

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
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Route = table.Column<string>(
                    type: "character varying(256)",
                    maxLength: 256,
                    nullable: false
                ),
                AllowedRoles = table.Column<List<string>>(type: "text[]", nullable: false),
                AllowedPermissions = table.Column<List<string>>(type: "text[]", nullable: false),
                CreatedAt = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
                UpdatedAt = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EndpointPermissions", x => x.Id);
            }
        );
    }
}
