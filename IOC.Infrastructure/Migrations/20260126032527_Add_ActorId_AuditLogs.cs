using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_ActorId_AuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ActorId",
                table: "audit_logs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_accounts_OrganizationId",
                table: "admin_accounts",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_accounts_Organizations_OrganizationId",
                table: "admin_accounts",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_accounts_Organizations_OrganizationId",
                table: "admin_accounts");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_ActorId",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_admin_accounts_OrganizationId",
                table: "admin_accounts");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "audit_logs");
        }
    }
}
