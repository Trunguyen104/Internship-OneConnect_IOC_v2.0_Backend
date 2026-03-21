using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInternshipApplicationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_internship_groups_internship_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_internship_id_student_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id",
                table: "internship_applications");

            migrationBuilder.RenameColumn(
                name: "internship_id",
                table: "internship_applications",
                newName: "term_id");

            migrationBuilder.AddColumn<Guid>(
                name: "enterprise_id",
                table: "internship_applications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "internship_group_internship_id",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reject_reason",
                table: "internship_applications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_internship_group_internship_id",
                table: "internship_applications",
                column: "internship_group_internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications",
                columns: new[] { "student_id", "term_id", "enterprise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_term_id",
                table: "internship_applications",
                column: "term_id");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_enterprises_enterprise_id",
                table: "internship_applications",
                column: "enterprise_id",
                principalTable: "enterprises",
                principalColumn: "enterprise_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_internship_groups_internship_group_",
                table: "internship_applications",
                column: "internship_group_internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_terms_term_id",
                table: "internship_applications",
                column: "term_id",
                principalTable: "terms",
                principalColumn: "term_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_enterprises_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_internship_groups_internship_group_",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_terms_term_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_internship_group_internship_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_student_id_term_id_enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_term_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "enterprise_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "internship_group_internship_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "internship_applications");

            migrationBuilder.RenameColumn(
                name: "term_id",
                table: "internship_applications",
                newName: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_internship_id_student_id",
                table: "internship_applications",
                columns: new[] { "internship_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id",
                table: "internship_applications",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_internship_groups_internship_id",
                table: "internship_applications",
                column: "internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id");
        }
    }
}
