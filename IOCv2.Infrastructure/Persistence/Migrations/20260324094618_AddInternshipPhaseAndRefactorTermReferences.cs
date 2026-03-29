using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipPhaseAndRefactorTermReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_evaluation_cycles_terms_term_id",
                table: "evaluation_cycles");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_groups_terms_term_id",
                table: "internship_groups");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "enterprises");

            migrationBuilder.RenameColumn(
                name: "term_id",
                table: "internship_groups",
                newName: "phase_id");

            migrationBuilder.RenameIndex(
                name: "ix_internship_groups_term_id",
                table: "internship_groups",
                newName: "ix_internship_groups_phase_id");

            migrationBuilder.RenameColumn(
                name: "term_id",
                table: "evaluation_cycles",
                newName: "phase_id");

            migrationBuilder.RenameIndex(
                name: "ix_evaluation_cycles_term_id",
                table: "evaluation_cycles",
                newName: "ix_evaluation_cycles_phase_id");

            migrationBuilder.CreateTable(
                name: "internship_phases",
                columns: table => new
                {
                    phase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enterprise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    max_students = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internship_phases", x => x.phase_id);
                    table.ForeignKey(
                        name: "fk_internship_phases_enterprises_enterprise_id",
                        column: x => x.enterprise_id,
                        principalTable: "enterprises",
                        principalColumn: "enterprise_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_dates",
                table: "internship_phases",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_id",
                table: "internship_phases",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_status",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases",
                columns: new[] { "enterprise_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            // ────────────────────────────────────────────────────────────────
            // Dọn dữ liệu orphan theo đúng thứ tự topological (leaf → root)
            // vì internship_phases mới hoàn toàn rỗng, mọi dòng trong
            // evaluation_cycles / internship_groups đang trỏ về terms cũ
            // → phải xóa toàn bộ cây con có FK RESTRICT trước khi AddForeignKey
            // ────────────────────────────────────────────────────────────────

            // Cây evaluation_cycles:
            //   evaluation_details (Cascade) → evaluations (Restrict) → evaluation_cycles
            //   evaluation_criteria (Cascade) → evaluation_cycles
            migrationBuilder.Sql(@"
                DELETE FROM evaluation_details
                WHERE evaluation_id IN (
                    SELECT evaluation_id FROM evaluations
                    WHERE cycle_id IN (
                        SELECT cycle_id FROM evaluation_cycles
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM evaluations
                WHERE cycle_id IN (
                    SELECT cycle_id FROM evaluation_cycles
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            migrationBuilder.Sql(@"
                DELETE FROM evaluation_criteria
                WHERE cycle_id IN (
                    SELECT cycle_id FROM evaluation_cycles
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            migrationBuilder.Sql(@"
                DELETE FROM evaluation_cycles
                WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases);");

            // Cây internship_groups:
            //   stakeholder_issues (Cascade) → stakeholders (Restrict) → internship_groups
            //   violation_reports (Restrict) → internship_groups
            //   evaluation_details (Cascade) → evaluations (Restrict,via InternshipId) → internship_groups
            //   logbook_work_items (Cascade) → logbooks (Cascade) → internship_groups
            //   sprint_work_items (Cascade) → sprints (Cascade) → projects (Restrict) → internship_groups
            //   project_resources (Cascade) → projects (Restrict) → internship_groups
            //   work_items (Cascade) → projects (Restrict) → internship_groups
            //   internship_students (Cascade) → internship_groups

            // 1. Tính tập internship_ids cần xóa
            // 2. Xóa leaf nodes trước

            // evaluation_details via evaluations.InternshipId
            migrationBuilder.Sql(@"
                DELETE FROM evaluation_details
                WHERE evaluation_id IN (
                    SELECT evaluation_id FROM evaluations
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            // evaluations via InternshipId
            migrationBuilder.Sql(@"
                DELETE FROM evaluations
                WHERE internship_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // stakeholder_issues → stakeholders
            migrationBuilder.Sql(@"
                DELETE FROM stakeholder_issues
                WHERE stakeholder_id IN (
                    SELECT stakeholder_id FROM stakeholders
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM stakeholders
                WHERE internship_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // violation_reports
            migrationBuilder.Sql(@"
                DELETE FROM violation_reports
                WHERE internship_group_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // logbook_work_items → logbooks
            migrationBuilder.Sql(@"
                DELETE FROM logbook_work_items
                WHERE logbook_id IN (
                    SELECT logbook_id FROM logbooks
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM logbooks
                WHERE internship_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // sprint_work_items → sprints → projects; project_resources → projects; work_items → projects
            migrationBuilder.Sql(@"
                DELETE FROM sprint_work_items
                WHERE sprint_id IN (
                    SELECT sprint_id FROM sprints
                    WHERE project_id IN (
                        SELECT project_id FROM projects
                        WHERE internship_id IN (
                            SELECT internship_id FROM internship_groups
                            WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                        )
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM sprints
                WHERE project_id IN (
                    SELECT project_id FROM projects
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM project_resources
                WHERE project_id IN (
                    SELECT project_id FROM projects
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            // work_items có self-referencing parent (Restrict) — xóa children trước
            migrationBuilder.Sql(@"
                DELETE FROM work_items
                WHERE project_id IN (
                    SELECT project_id FROM projects
                    WHERE internship_id IN (
                        SELECT internship_id FROM internship_groups
                        WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                    )
                );");

            migrationBuilder.Sql(@"
                DELETE FROM projects
                WHERE internship_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // internship_applications (nullable FK, SET NULL không cần xóa — để nguyên)
            // internship_students (Cascade — xóa trước internship_groups)
            migrationBuilder.Sql(@"
                DELETE FROM internship_students
                WHERE internship_id IN (
                    SELECT internship_id FROM internship_groups
                    WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases)
                );");

            // Cuối cùng: xóa internship_groups orphan
            migrationBuilder.Sql(@"
                DELETE FROM internship_groups
                WHERE phase_id NOT IN (SELECT phase_id FROM internship_phases);");

            // ── Thêm FK mới sau khi đã dọn sạch dữ liệu orphan ──
            migrationBuilder.AddForeignKey(
                name: "fk_evaluation_cycles_internship_phases_phase_id",
                table: "evaluation_cycles",
                column: "phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups",
                column: "phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_evaluation_cycles_internship_phases_phase_id",
                table: "evaluation_cycles");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_groups_internship_phases_phase_id",
                table: "internship_groups");

            migrationBuilder.DropIndex(
                name: "ix_internship_phases_enterprise_name_unique",
                table: "internship_phases");

            migrationBuilder.DropTable(
                name: "internship_phases");

            migrationBuilder.RenameColumn(
                name: "phase_id",
                table: "internship_groups",
                newName: "term_id");

            migrationBuilder.RenameIndex(
                name: "ix_internship_groups_phase_id",
                table: "internship_groups",
                newName: "ix_internship_groups_term_id");

            migrationBuilder.RenameColumn(
                name: "phase_id",
                table: "evaluation_cycles",
                newName: "term_id");

            migrationBuilder.RenameIndex(
                name: "ix_evaluation_cycles_phase_id",
                table: "evaluation_cycles",
                newName: "ix_evaluation_cycles_term_id");

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "enterprises",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "fk_evaluation_cycles_terms_term_id",
                table: "evaluation_cycles",
                column: "term_id",
                principalTable: "terms",
                principalColumn: "term_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_groups_terms_term_id",
                table: "internship_groups",
                column: "term_id",
                principalTable: "terms",
                principalColumn: "term_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
