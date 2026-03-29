using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TwoLayerProjectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_projects_status",
                table: "projects");

            // Step 1: Add new columns first (with default 0 = Draft/Unstarted)
            migrationBuilder.AddColumn<short>(
                name: "operational_status",
                table: "projects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "visibility_status",
                table: "projects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Step 2: Data migration — map old status values to new two-layer model
            migrationBuilder.Sql(@"
                UPDATE projects SET
                    visibility_status = CASE status
                        WHEN 0 THEN 0  -- Draft    → visibility=Draft
                        WHEN 1 THEN 1  -- Published → visibility=Published
                        WHEN 2 THEN 1  -- Completed → visibility=Published
                        WHEN 3 THEN 1  -- Archived  → visibility=Published
                        ELSE 0
                    END,
                    operational_status = CASE status
                        WHEN 0 THEN 0  -- Draft    → operational=Unstarted
                        WHEN 1 THEN 0  -- Published → operational=Unstarted (no group yet)
                        WHEN 2 THEN 2  -- Completed → operational=Completed
                        WHEN 3 THEN 3  -- Archived  → operational=Archived
                        ELSE 0
                    END
                WHERE deleted_at IS NULL OR deleted_at IS NOT NULL
            ");

            // Step 3: Drop old status column after data has been migrated
            migrationBuilder.DropColumn(
                name: "status",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "ix_projects_operational_status",
                table: "projects",
                column: "operational_status");

            migrationBuilder.CreateIndex(
                name: "ix_projects_visibility_status",
                table: "projects",
                column: "visibility_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_projects_operational_status",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "ix_projects_visibility_status",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "operational_status",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "visibility_status",
                table: "projects");

            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "projects",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                table: "projects",
                column: "status");
        }
    }
}
