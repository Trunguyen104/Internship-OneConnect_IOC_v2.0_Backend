using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "projects",
                newName: "project_name");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "projects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "end_date",
                table: "projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "internship_id",
                table: "projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "mentor_id",
                table: "projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                table: "projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "projects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_date",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "internship_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "mentor_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "status",
                table: "projects");

            migrationBuilder.RenameColumn(
                name: "project_name",
                table: "projects",
                newName: "name");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "projects",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
