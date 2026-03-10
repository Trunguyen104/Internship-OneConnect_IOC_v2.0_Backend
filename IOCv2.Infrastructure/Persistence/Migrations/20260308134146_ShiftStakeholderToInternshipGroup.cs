using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShiftStakeholderToInternshipGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stakeholders_projects",
                table: "stakeholders");

            migrationBuilder.RenameColumn(
                name: "project_id",
                table: "stakeholders",
                newName: "internship_id");

            migrationBuilder.Sql(@"
                UPDATE stakeholders 
                SET internship_id = p.internship_id
                FROM projects p
                WHERE stakeholders.internship_id = p.project_id;
            ");

            migrationBuilder.RenameIndex(
                name: "ix_stakeholders_project_id",
                table: "stakeholders",
                newName: "ix_stakeholders_internship_id");

            migrationBuilder.RenameIndex(
                name: "ix_stakeholders_project_email_unique",
                table: "stakeholders",
                newName: "ix_stakeholders_internship_email_unique");

            migrationBuilder.AddForeignKey(
                name: "fk_stakeholders_internship_groups",
                table: "stakeholders",
                column: "internship_id",
                principalTable: "internship_groups",
                principalColumn: "internship_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stakeholders_internship_groups",
                table: "stakeholders");

            migrationBuilder.RenameColumn(
                name: "internship_id",
                table: "stakeholders",
                newName: "project_id");

            migrationBuilder.Sql(@"
                UPDATE stakeholders
                SET project_id = p.project_id
                FROM projects p
                WHERE stakeholders.project_id = p.internship_id;
            ");

            migrationBuilder.RenameIndex(
                name: "ix_stakeholders_internship_id",
                table: "stakeholders",
                newName: "ix_stakeholders_project_id");

            migrationBuilder.RenameIndex(
                name: "ix_stakeholders_internship_email_unique",
                table: "stakeholders",
                newName: "ix_stakeholders_project_email_unique");

            migrationBuilder.AddForeignKey(
                name: "fk_stakeholders_projects",
                table: "stakeholders",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "project_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
