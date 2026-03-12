using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloseReasonToTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "close_reason",
                table: "terms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "close_reason",
                table: "terms");
        }
    }
}

