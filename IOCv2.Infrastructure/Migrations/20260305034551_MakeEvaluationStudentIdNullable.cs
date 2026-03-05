using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeEvaluationStudentIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_evaluations_cycle_student_unique",
                table: "evaluations");

            migrationBuilder.AlterColumn<Guid>(
                name: "student_id",
                table: "evaluations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "internship_id", "student_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations");

            migrationBuilder.AlterColumn<Guid>(
                name: "student_id",
                table: "evaluations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "student_id" },
                unique: true);
        }
    }
}
