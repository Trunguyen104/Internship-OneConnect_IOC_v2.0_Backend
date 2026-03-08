using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationAndDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluations", x => x.evaluation_id);
                    table.ForeignKey(
                        name: "fk_evaluations_evaluation_cycles_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "evaluation_cycles",
                        principalColumn: "cycle_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_users_evaluator_id",
                        column: x => x.evaluator_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_details",
                columns: table => new
                {
                    detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criteria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluation_details", x => x.detail_id);
                    table.ForeignKey(
                        name: "fk_evaluation_details_evaluation_criteria_criteria_id",
                        column: x => x.criteria_id,
                        principalTable: "evaluation_criteria",
                        principalColumn: "criteria_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluation_details_evaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "evaluations",
                        principalColumn: "evaluation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_criteria_id",
                table: "evaluation_details",
                column: "criteria_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details",
                columns: new[] { "evaluation_id", "criteria_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_evaluator_id",
                table: "evaluations",
                column: "evaluator_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_internship_id",
                table: "evaluations",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_student_id",
                table: "evaluations",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evaluation_details");

            migrationBuilder.DropTable(
                name: "evaluations");
        }
    }
}
