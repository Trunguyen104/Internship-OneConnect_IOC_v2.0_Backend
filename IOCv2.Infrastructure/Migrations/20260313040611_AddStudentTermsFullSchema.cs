using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentTermsFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_students_student_id",
                table: "student_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_terms_term_id",
                table: "student_terms");

            migrationBuilder.DropPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms");

            // Drop status column only if it exists (DB may already be in a different state)
            migrationBuilder.Sql("ALTER TABLE student_terms DROP COLUMN IF EXISTS status;");

            migrationBuilder.RenameIndex(
                name: "ix_student_terms_term_id",
                table: "student_terms",
                newName: "idx_student_terms_term_id");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "student_terms",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateOnly>(
                name: "enrollment_date",
                table: "student_terms",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.AddColumn<string>(
                name: "enrollment_note",
                table: "student_terms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "enrollment_status",
                table: "student_terms",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<Guid>(
                name: "enterprise_id",
                table: "student_terms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_feedback",
                table: "student_terms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midterm_feedback",
                table: "student_terms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "placement_status",
                table: "student_terms",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "idx_student_terms_statuses",
                table: "student_terms",
                columns: new[] { "term_id", "placement_status", "enrollment_status" });

            migrationBuilder.CreateIndex(
                name: "ix_student_terms_enterprise_id",
                table: "student_terms",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "uq_student_term",
                table: "student_terms",
                columns: new[] { "student_id", "term_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_enterprises_enterprise_id",
                table: "student_terms",
                column: "enterprise_id",
                principalTable: "enterprises",
                principalColumn: "enterprise_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_students_student_id",
                table: "student_terms",
                column: "student_id",
                principalTable: "students",
                principalColumn: "student_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_terms_term_id",
                table: "student_terms",
                column: "term_id",
                principalTable: "terms",
                principalColumn: "term_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_enterprises_enterprise_id",
                table: "student_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_students_student_id",
                table: "student_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_terms_term_id",
                table: "student_terms");

            migrationBuilder.DropPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms");

            migrationBuilder.DropIndex(
                name: "idx_student_terms_statuses",
                table: "student_terms");

            migrationBuilder.DropIndex(
                name: "ix_student_terms_enterprise_id",
                table: "student_terms");

            migrationBuilder.DropIndex(
                name: "uq_student_term",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "id",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "enrollment_date",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "enrollment_note",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "enrollment_status",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "enterprise_id",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "final_feedback",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "midterm_feedback",
                table: "student_terms");

            migrationBuilder.DropColumn(
                name: "placement_status",
                table: "student_terms");

            migrationBuilder.RenameIndex(
                name: "idx_student_terms_term_id",
                table: "student_terms",
                newName: "ix_student_terms_term_id");

            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "student_terms",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms",
                columns: new[] { "student_id", "term_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_students_student_id",
                table: "student_terms",
                column: "student_id",
                principalTable: "students",
                principalColumn: "student_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_terms_term_id",
                table: "student_terms",
                column: "term_id",
                principalTable: "terms",
                principalColumn: "term_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
