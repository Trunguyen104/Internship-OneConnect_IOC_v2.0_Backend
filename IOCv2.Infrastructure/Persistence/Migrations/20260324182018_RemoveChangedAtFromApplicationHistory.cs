using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChangedAtFromApplicationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_application_status_histories_changed_at",
                table: "application_status_histories");

            migrationBuilder.DropColumn(
                name: "changed_at",
                table: "application_status_histories");

            migrationBuilder.CreateIndex(
                name: "ix_application_status_histories_created_at",
                table: "application_status_histories",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_application_status_histories_created_at",
                table: "application_status_histories");

            migrationBuilder.AddColumn<DateTime>(
                name: "changed_at",
                table: "application_status_histories",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "ix_application_status_histories_changed_at",
                table: "application_status_histories",
                column: "changed_at");
        }
    }
}
