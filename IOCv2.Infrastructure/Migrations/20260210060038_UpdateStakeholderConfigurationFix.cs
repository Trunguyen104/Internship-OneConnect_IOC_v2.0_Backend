using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStakeholderConfigurationFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stakeholders_project_email_unique",
                table: "stakeholders");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholders_project_email_unique",
                table: "stakeholders",
                columns: new[] { "project_id", "email" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stakeholders_project_email_unique",
                table: "stakeholders");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholders_project_email_unique",
                table: "stakeholders",
                columns: new[] { "project_id", "email" },
                unique: true);
        }
    }
}
