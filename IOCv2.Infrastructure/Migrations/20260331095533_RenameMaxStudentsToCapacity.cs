using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaxStudentsToCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases");

            migrationBuilder.RenameColumn(
                name: "max_students",
                table: "internship_phases",
                newName: "capacity");

            migrationBuilder.Sql("UPDATE internship_phases SET capacity = 0 WHERE capacity IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "capacity",
                table: "internship_phases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "major_fields",
                table: "internship_phases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_name",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "name" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_internship_phases_enterprise_name",
                table: "internship_phases");

            migrationBuilder.DropColumn(
                name: "major_fields",
                table: "internship_phases");

            migrationBuilder.AlterColumn<int>(
                name: "capacity",
                table: "internship_phases",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.RenameColumn(
                name: "capacity",
                table: "internship_phases",
                newName: "max_students");

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}
