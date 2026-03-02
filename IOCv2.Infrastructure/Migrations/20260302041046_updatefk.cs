using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatefk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewer_enterpris",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_reviewer_enterprise_user_id",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "reviewer_enterprise_user_id",
                table: "internship_applications");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_enterprise_users_reviewed_by",
                table: "internship_applications");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_reviewed_by",
                table: "internship_applications");

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
        }
    }
}
