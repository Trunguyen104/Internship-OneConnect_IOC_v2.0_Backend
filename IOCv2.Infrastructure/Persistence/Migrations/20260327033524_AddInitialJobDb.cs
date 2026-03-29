using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialJobDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "internship_duration",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "created_at1",
                table: "internship_applications");

            migrationBuilder.AddColumn<short>(
                name: "audience",
                table: "jobs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "position",
                table: "jobs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.AddColumn<string>(
                name: "cv_snapshot_url",
                table: "internship_applications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                table: "internship_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "job_posting_title",
                table: "internship_applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "university_id",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_universities",
                columns: table => new
                {
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uni_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_universities", x => new { x.job_id, x.uni_id });
                    table.ForeignKey(
                        name: "fk_job_universities_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "job_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_job_universities_university_id",
                        column: x => x.uni_id,
                        principalTable: "universities",
                        principalColumn: "uni_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id_status",
                table: "internship_applications",
                columns: new[] { "enterprise_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_job_id",
                table: "internship_applications",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_status",
                table: "internship_applications",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_university_id",
                table: "internship_applications",
                column: "university_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_universities_job_id",
                table: "job_universities",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_universities_uni_id",
                table: "job_universities",
                column: "uni_id");

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

            migrationBuilder.DropTable(
                name: "job_universities");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id_status",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_job_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_status",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_university_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "audience",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "position",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "applied_at",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "cv_snapshot_url",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "job_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "job_posting_title",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "university_id",
                table: "internship_applications");

            migrationBuilder.AddColumn<int>(
                name: "internship_duration",
                table: "jobs",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "job_applications",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    cover_letter = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_snapshot_file_name = table.Column<string>(type: "text", nullable: true),
                    cv_snapshot_url = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_applications", x => x.application_id);
                    table.ForeignKey(
                        name: "fk_job_applications_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "job_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_applications_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_job_id",
                table: "job_applications",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_job_id_student_id",
                table: "job_applications",
                columns: new[] { "job_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_student_id",
                table: "job_applications",
                column: "student_id");
        }
    }
}
