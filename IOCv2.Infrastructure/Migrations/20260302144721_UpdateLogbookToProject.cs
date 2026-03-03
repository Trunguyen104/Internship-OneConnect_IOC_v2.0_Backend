using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLogbookToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewer_enterpris",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_logbooks_internship_groups_internship_id",
                table: "logbooks");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_reviewer_enterprise_user_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "reviewer_enterprise_user_id",
                table: "internship_applications");

            migrationBuilder.RenameColumn(
                name: "internship_id",
                table: "logbooks",
                newName: "project_id");

            migrationBuilder.RenameIndex(
                name: "ix_logbooks_internship_id",
                table: "logbooks",
                newName: "ix_logbooks_project_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_reviewed_by",
                table: "internship_applications",
                column: "reviewed_by");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewed_by",
                table: "internship_applications",
                column: "reviewed_by",
                principalTable: "enterprise_users",
                principalColumn: "enterprise_user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_logbooks_projects_project_id",
                table: "logbooks",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "project_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewed_by",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_logbooks_projects_project_id",
                table: "logbooks");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_reviewed_by",
                table: "internship_applications");

            migrationBuilder.RenameColumn(
                name: "project_id",
                table: "logbooks",
                newName: "internship_id");

            migrationBuilder.RenameIndex(
                name: "ix_logbooks_project_id",
                table: "logbooks",
                newName: "ix_logbooks_internship_id");

            migrationBuilder.AddColumn<Guid>(
                name: "reviewer_enterprise_user_id",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_reviewer_enterprise_user_id",
                table: "internship_applications",
                column: "reviewer_enterprise_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewer_enterpris",
                table: "internship_applications",
                column: "reviewer_enterprise_user_id",
                principalTable: "enterprise_users",
                principalColumn: "enterprise_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_logbooks_internship_groups_internship_id",
                table: "logbooks",
                column: "internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
