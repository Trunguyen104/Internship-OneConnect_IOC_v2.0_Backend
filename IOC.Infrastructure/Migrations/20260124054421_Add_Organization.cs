using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Organization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitId",
                table: "admin_accounts",
                newName: "OrganizationId");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "admin_accounts",
                newName: "UnitId");
        }
    }
}
