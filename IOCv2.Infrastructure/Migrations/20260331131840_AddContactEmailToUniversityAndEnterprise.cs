using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactEmailToUniversityAndEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "universities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "enterprises",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "universities");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "enterprises");
        }
    }
}
