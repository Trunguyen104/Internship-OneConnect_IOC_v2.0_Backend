using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RewriteStudentTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa indexes cũ
            migrationBuilder.DropIndex(
                name: "ix_student_terms_term_id",
                table: "student_terms");

            // 2. Xóa Foreign Keys cũ
            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_students_student_id",
                table: "student_terms");

            migrationBuilder.DropForeignKey(
                name: "fk_student_terms_terms_term_id",
                table: "student_terms");

            // 3. Xóa Primary Key composite cũ (student_id, term_id)
            migrationBuilder.DropPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms");

            // 4. Xóa cột status cũ
            migrationBuilder.DropColumn(
                name: "status",
                table: "student_terms");

            // 5. Thêm cột student_term_id làm PK mới (uuid, gen_random_uuid)
            migrationBuilder.AddColumn<Guid>(
                name: "student_term_id",
                table: "student_terms",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            // 6. Đặt PK mới
            migrationBuilder.AddPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms",
                column: "student_term_id");

            // 7. Thêm cột enterprise_id
            migrationBuilder.AddColumn<Guid>(
                name: "enterprise_id",
                table: "student_terms",
                type: "uuid",
                nullable: true);

            // 8. Thêm enrollment_status (smallint, default 1 = Active)
            migrationBuilder.AddColumn<short>(
                name: "enrollment_status",
                table: "student_terms",
                type: "smallint",
                nullable: false,
                defaultValue: (short)IOCv2.Domain.Enums.EnrollmentStatus.Active);

            // 9. Thêm placement_status (smallint, default 0 = Unplaced)
            migrationBuilder.AddColumn<short>(
                name: "placement_status",
                table: "student_terms",
                type: "smallint",
                nullable: false,
                defaultValue: (short)IOCv2.Domain.Enums.PlacementStatus.Unplaced);

            // 10. Thêm enrollment_date (date, default CURRENT_DATE)
            migrationBuilder.AddColumn<DateOnly>(
                name: "enrollment_date",
                table: "student_terms",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            // 11. Thêm các cột text
            migrationBuilder.AddColumn<string>(
                name: "enrollment_note",
                table: "student_terms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midterm_feedback",
                table: "student_terms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_feedback",
                table: "student_terms",
                type: "text",
                nullable: true);

            // 12. Thêm deleted_by
            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                table: "student_terms",
                type: "uuid",
                nullable: true);

            // 13. Tạo lại Foreign Keys với behavior mới
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

            migrationBuilder.AddForeignKey(
                name: "fk_student_terms_enterprises_enterprise_id",
                table: "student_terms",
                column: "enterprise_id",
                principalTable: "enterprises",
                principalColumn: "enterprise_id",
                onDelete: ReferentialAction.SetNull);

            // 14. Tạo Partial Unique Index: 1 SV chỉ có 1 record trong 1 kỳ (nếu chưa xóa)
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX uq_student_term
                  ON student_terms (student_id, term_id)
                  WHERE deleted_at IS NULL;");

            // 15. Tạo Index tối ưu query list theo kỳ
            migrationBuilder.CreateIndex(
                name: "idx_student_terms_term_id",
                table: "student_terms",
                column: "term_id");

            // 16. Tạo Index tối ưu đếm + filter theo trạng thái
            migrationBuilder.CreateIndex(
                name: "idx_student_terms_statuses",
                table: "student_terms",
                columns: new[] { "term_id", "placement_status", "enrollment_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa indexes mới
            migrationBuilder.DropIndex(name: "uq_student_term", table: "student_terms");
            migrationBuilder.DropIndex(name: "idx_student_terms_term_id", table: "student_terms");
            migrationBuilder.DropIndex(name: "idx_student_terms_statuses", table: "student_terms");

            // Xóa FKs mới
            migrationBuilder.DropForeignKey(name: "fk_student_terms_students_student_id", table: "student_terms");
            migrationBuilder.DropForeignKey(name: "fk_student_terms_terms_term_id", table: "student_terms");
            migrationBuilder.DropForeignKey(name: "fk_student_terms_enterprises_enterprise_id", table: "student_terms");

            // Xóa PK mới
            migrationBuilder.DropPrimaryKey(name: "pk_student_terms", table: "student_terms");

            // Xóa các cột mới
            migrationBuilder.DropColumn(name: "student_term_id", table: "student_terms");
            migrationBuilder.DropColumn(name: "enterprise_id", table: "student_terms");
            migrationBuilder.DropColumn(name: "enrollment_status", table: "student_terms");
            migrationBuilder.DropColumn(name: "placement_status", table: "student_terms");
            migrationBuilder.DropColumn(name: "enrollment_date", table: "student_terms");
            migrationBuilder.DropColumn(name: "enrollment_note", table: "student_terms");
            migrationBuilder.DropColumn(name: "midterm_feedback", table: "student_terms");
            migrationBuilder.DropColumn(name: "final_feedback", table: "student_terms");
            migrationBuilder.DropColumn(name: "deleted_by", table: "student_terms");

            // Khôi phục cột status cũ
            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "student_terms",
                type: "smallint",
                nullable: true);

            // Khôi phục PK composite cũ
            migrationBuilder.AddPrimaryKey(
                name: "pk_student_terms",
                table: "student_terms",
                columns: new[] { "student_id", "term_id" });

            // Khôi phục FKs cũ
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

            // Khôi phục index cũ
            migrationBuilder.CreateIndex(
                name: "ix_student_terms_term_id",
                table: "student_terms",
                column: "term_id");
        }
    }
}
