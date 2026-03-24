using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotJobApplicationDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cv_snapshot_file_name",
                table: "job_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_snapshot_url",
                table: "job_applications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cv_snapshot_file_name",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "cv_snapshot_url",
                table: "job_applications");
        }
    }
}
