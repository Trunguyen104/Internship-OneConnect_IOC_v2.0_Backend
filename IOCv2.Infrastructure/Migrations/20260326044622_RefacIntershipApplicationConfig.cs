using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefacIntershipApplicationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_jobs_job_id",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_universities_university_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "created_at1",
                table: "internship_applications");

            migrationBuilder.AlterColumn<string>(
                name: "reject_reason",
                table: "internship_applications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "job_posting_title",
                table: "internship_applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cv_snapshot_url",
                table: "internship_applications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "internship_applications",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "applied_at",
                table: "internship_applications",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id_status",
                table: "internship_applications",
                columns: new[] { "enterprise_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_status",
                table: "internship_applications",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_jobs_job_id",
                table: "internship_applications",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_universities_university_id",
                table: "internship_applications",
                column: "university_id",
                principalTable: "universities",
                principalColumn: "uni_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_jobs_job_id",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_universities_university_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id_status",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_status",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "applied_at",
                table: "internship_applications");

            migrationBuilder.AlterColumn<string>(
                name: "reject_reason",
                table: "internship_applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "job_posting_title",
                table: "internship_applications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cv_snapshot_url",
                table: "internship_applications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "internship_applications",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at1",
                table: "internship_applications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_jobs_job_id",
                table: "internship_applications",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "job_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_universities_university_id",
                table: "internship_applications",
                column: "university_id",
                principalTable: "universities",
                principalColumn: "uni_id");
        }
    }
}
