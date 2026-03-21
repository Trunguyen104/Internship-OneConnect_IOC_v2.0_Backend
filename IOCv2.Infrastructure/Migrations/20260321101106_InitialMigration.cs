using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_projects_internship_groups_internship_group_internship_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_projects_internship_group_internship_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "internship_group_internship_id",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "ix_student_terms_enterprise_id",
                table: "student_terms",
                column: "enterprise_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_student_terms_enterprise_id",
                table: "student_terms");

            migrationBuilder.AddColumn<Guid>(
                name: "internship_group_internship_id",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_internship_group_internship_id",
                table: "projects",
                column: "internship_group_internship_id");

            migrationBuilder.AddForeignKey(
                name: "fk_projects_internship_groups_internship_group_internship_id",
                table: "projects",
                column: "internship_group_internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id");
        }
    }
}
