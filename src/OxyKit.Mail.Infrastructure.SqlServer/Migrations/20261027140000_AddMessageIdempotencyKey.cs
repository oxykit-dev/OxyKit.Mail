using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OxyKit.Mail.Infrastructure.SqlServer.Migrations;

[DbContext(typeof(OxyKit.Mail.Infrastructure.ApplicationDbContext))]
[Migration("20261027140000_AddMessageIdempotencyKey")]
public partial class AddMessageIdempotencyKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "Messages",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Messages_IdempotencyKey",
            table: "Messages",
            column: "IdempotencyKey",
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Messages_IdempotencyKey", table: "Messages");

        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "Messages");
    }
}
