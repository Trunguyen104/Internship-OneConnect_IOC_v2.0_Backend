using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInternPhaseIdToJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "intern_phase_id",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_jobs_intern_phase_id",
                table: "jobs",
                column: "intern_phase_id");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_phase_id",
                table: "jobs",
                column: "intern_phase_id",
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

            migrationBuilder.DropIndex(
                name: "ix_jobs_intern_phase_id",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "intern_phase_id",
                table: "jobs");
        }
    }
}
