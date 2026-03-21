using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddViolationReportDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "violation_reports",
                columns: table => new
                {
                    violation_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violation_reports", x => x.violation_report_id);
                    table.ForeignKey(
                        name: "fk_violation_reports_internship_groups_internship_group_id",
                        column: x => x.internship_group_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_violation_reports_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_violation_reports_internship_group_id",
                table: "violation_reports",
                column: "internship_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_reports_occurred_date",
                table: "violation_reports",
                column: "occurred_date");

            migrationBuilder.CreateIndex(
                name: "ix_violation_reports_student_id",
                table: "violation_reports",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "violation_reports");
        }
    }
}
