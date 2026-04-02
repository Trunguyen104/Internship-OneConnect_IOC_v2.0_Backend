using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
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
