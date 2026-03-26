using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectAssignmentsMakeInternshipIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_projects_internship_groups_internship_id",
                table: "projects");

            migrationBuilder.AddColumn<string>(
                name: "deliverables",
                table: "projects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "field",
                table: "projects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "mentor_id",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project_code",
                table: "projects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "requirements",
                table: "projects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "template",
                table: "projects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "uploaded_at",
                table: "project_resources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "uploaded_by",
                table: "project_resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_id",
                table: "projects",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql(@"
                UPDATE projects
                SET project_code = 'PRJ-' || substring(replace(project_id::text, '-', ''), 1, 8)
                WHERE project_code IS NULL OR btrim(project_code) = '';

                WITH ranked AS (
                    SELECT project_id,
                           project_code,
                           row_number() OVER (PARTITION BY project_code ORDER BY created_at NULLS LAST, project_id) AS rn
                    FROM projects
                    WHERE deleted_at IS NULL
                )
                UPDATE projects p
                SET project_code = left(r.project_code, 40) || '-' || substring(replace(p.project_id::text, '-', ''), 1, 8)
                FROM ranked r
                WHERE p.project_id = r.project_id
                  AND r.rn > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "ix_projects_mentor_id",
                table: "projects",
                column: "mentor_id");

            migrationBuilder.CreateIndex(
                name: "uix_projects_project_code_active",
                table: "projects",
                column: "project_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_project_resources_uploaded_by",
                table: "project_resources",
                column: "uploaded_by");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups",
                column: "phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_resources_enterprise_users_uploaded_by",
                table: "project_resources",
                column: "uploaded_by",
                principalTable: "enterprise_users",
                principalColumn: "enterprise_user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_projects_enterprise_users_mentor_id",
                table: "projects",
                column: "mentor_id",
                principalTable: "enterprise_users",
                principalColumn: "enterprise_user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_projects_internship_groups_internship_id",
                table: "projects",
                column: "internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups");

            migrationBuilder.DropForeignKey(
                name: "fk_project_resources_enterprise_users_uploaded_by",
                table: "project_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_projects_enterprise_users_mentor_id",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "fk_projects_internship_groups_internship_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_projects_mentor_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "uix_projects_project_code_active",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_project_resources_uploaded_by",
                table: "project_resources");

            migrationBuilder.DropColumn(
                name: "deliverables",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "field",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "mentor_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "project_code",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "requirements",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "template",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "uploaded_at",
                table: "project_resources");

            migrationBuilder.DropColumn(
                name: "uploaded_by",
                table: "project_resources");

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_id",
                table: "projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups",
                column: "phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_projects_internship_groups_internship_id",
                table: "projects",
                column: "internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
