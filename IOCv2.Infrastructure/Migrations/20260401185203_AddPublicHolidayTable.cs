using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicHolidayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_jobs_job_id1",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_job_id1",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "job_id1",
                table: "internship_applications");

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_phase_id",
                table: "jobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "public_holidays",
                columns: table => new
                {
                    public_holiday_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_public_holidays", x => x.public_holiday_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_public_holidays_date",
                table: "public_holidays",
                column: "date",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_phase_id",
                table: "jobs",
                column: "internship_phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_jobs_internship_phases_phase_id",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "public_holidays");

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_phase_id",
                table: "jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id1",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_job_id1",
                table: "internship_applications",
                column: "job_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_jobs_job_id1",
                table: "internship_applications",
                column: "job_id1",
                principalTable: "jobs",
                principalColumn: "job_id");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs",
                column: "internship_phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
