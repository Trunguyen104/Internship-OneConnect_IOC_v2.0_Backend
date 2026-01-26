using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Edit_Organization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
        name: "Type",
        table: "Organizations");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Organizations",
                type: "integer",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Organizations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
