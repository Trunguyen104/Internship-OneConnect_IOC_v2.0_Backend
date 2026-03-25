using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHRApplicationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "created_at1",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "is_verified",
                table: "enterprises");

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

            migrationBuilder.AddColumn<string>(
                name: "job_posting_title",
                table: "internship_applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "source",
                table: "internship_applications",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "university_id",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "application_status_histories",
                columns: table => new
                {
                    history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<short>(type: "smallint", nullable: false),
                    to_status = table.Column<short>(type: "smallint", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    trigger_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "HR"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_status_histories", x => x.history_id);
                    table.ForeignKey(
                        name: "fk_application_status_histories_internship_applications_applic",
                        column: x => x.application_id,
                        principalTable: "internship_applications",
                        principalColumn: "application_id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_university_id",
                table: "internship_applications",
                column: "university_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_status_histories_application_id",
                table: "application_status_histories",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_status_histories_changed_at",
                table: "application_status_histories",
                column: "changed_at");

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
                name: "fk_internship_applications_universities_university_id",
                table: "internship_applications");

            migrationBuilder.DropTable(
                name: "application_status_histories");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id_status",
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
                name: "applied_at",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "cv_snapshot_url",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "job_posting_title",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "source",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "university_id",
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

            migrationBuilder.AddColumn<bool>(
                name: "is_verified",
                table: "enterprises",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true);
        }
    }
}
