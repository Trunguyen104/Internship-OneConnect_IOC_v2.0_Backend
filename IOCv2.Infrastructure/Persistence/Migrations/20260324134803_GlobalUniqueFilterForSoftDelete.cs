using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GlobalUniqueFilterForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_phone_number",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_user_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_university_users_user_id",
                table: "university_users");

            migrationBuilder.DropIndex(
                name: "ix_universities_code",
                table: "universities");

            migrationBuilder.DropIndex(
                name: "ix_students_user_id",
                table: "students");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations");

            migrationBuilder.DropIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details");

            migrationBuilder.DropIndex(
                name: "ix_enterprises_tax_code",
                table: "enterprises");

            migrationBuilder.DropIndex(
                name: "ix_enterprise_users_user_id",
                table: "enterprise_users");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "enterprises");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_user_code",
                table: "users",
                column: "user_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_university_users_user_id",
                table: "university_users",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_universities_code",
                table: "universities",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_students_user_id",
                table: "students",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "internship_id", "student_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details",
                columns: new[] { "evaluation_id", "criteria_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_enterprises_tax_code",
                table: "enterprises",
                column: "tax_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_enterprise_users_user_id",
                table: "enterprise_users",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_phone_number",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_user_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_university_users_user_id",
                table: "university_users");

            migrationBuilder.DropIndex(
                name: "ix_universities_code",
                table: "universities");

            migrationBuilder.DropIndex(
                name: "ix_students_user_id",
                table: "students");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations");

            migrationBuilder.DropIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details");

            migrationBuilder.DropIndex(
                name: "ix_enterprises_tax_code",
                table: "enterprises");

            migrationBuilder.DropIndex(
                name: "ix_enterprise_users_user_id",
                table: "enterprise_users");

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "enterprises",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_university_users_user_id",
                table: "university_users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_universities_code",
                table: "universities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_user_id",
                table: "students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "internship_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details",
                columns: new[] { "evaluation_id", "criteria_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enterprises_tax_code",
                table: "enterprises",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enterprise_users_user_id",
                table: "enterprise_users",
                column: "user_id",
                unique: true);
        }
    }
}
