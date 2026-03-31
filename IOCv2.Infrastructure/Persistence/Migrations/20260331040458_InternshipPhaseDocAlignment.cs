using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InternshipPhaseDocAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases");

            migrationBuilder.AddColumn<Guid>(
                name: "phase_id",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "capacity",
                table: "internship_phases",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "major_fields",
                table: "internship_phases",
                type: "text",
                nullable: false,
                defaultValue: "General");

            migrationBuilder.Sql(@"
                UPDATE internship_phases
                SET capacity = GREATEST(COALESCE(max_students, 1), 1),
                    major_fields = CASE
                        WHEN major_fields IS NULL OR major_fields = '' THEN 'General'
                        ELSE major_fields
                    END;
            ");

            migrationBuilder.DropColumn(
                name: "max_students",
                table: "internship_phases");

            migrationBuilder.AlterColumn<Guid>(
                name: "phase_id",
                table: "internship_groups",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_phase_id",
                table: "jobs",
                column: "phase_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_internship_phases_capacity_min",
                table: "internship_phases",
                sql: "capacity >= 1");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_phase_id",
                table: "jobs",
                column: "phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_jobs_internship_phases_phase_id",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_jobs_phase_id",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases");

            migrationBuilder.DropCheckConstraint(
                name: "ck_internship_phases_capacity_min",
                table: "internship_phases");

            migrationBuilder.DropColumn(
                name: "phase_id",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "capacity",
                table: "internship_phases");

            migrationBuilder.DropColumn(
                name: "major_fields",
                table: "internship_phases");

            migrationBuilder.AddColumn<int>(
                name: "max_students",
                table: "internship_phases",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE internship_phases
                SET max_students = capacity;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "phase_id",
                table: "internship_groups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}
