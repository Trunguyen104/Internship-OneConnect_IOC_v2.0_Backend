using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMentorHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_internship_applications_jobs_job_id1",
                table: "internship_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "ix_internship_applications_job_id1",
                table: "internship_applications");

            migrationBuilder.DropColumn(
                name: "job_id1",
                table: "internship_applications");

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_phase_id",
                table: "jobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "group_mentor_history",
                columns: table => new
                {
                    history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_mentor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_mentor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<short>(type: "smallint", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_mentor_history", x => x.history_id);
                    table.ForeignKey(
                        name: "fk_group_mentor_history_enterprise_users_new_mentor_id",
                        column: x => x.new_mentor_id,
                        principalTable: "enterprise_users",
                        principalColumn: "enterprise_user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_mentor_history_enterprise_users_old_mentor_id",
                        column: x => x.old_mentor_id,
                        principalTable: "enterprise_users",
                        principalColumn: "enterprise_user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_mentor_history_internship_groups_internship_group_id",
                        column: x => x.internship_group_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_mentor_history_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_mentor_history_actor_id",
                table: "group_mentor_history",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_mentor_history_group_id",
                table: "group_mentor_history",
                column: "internship_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_mentor_history_new_mentor_id",
                table: "group_mentor_history",
                column: "new_mentor_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_mentor_history_old_mentor_id",
                table: "group_mentor_history",
                column: "old_mentor_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_mentor_history_timestamp",
                table: "group_mentor_history",
                column: "timestamp");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs",
                column: "internship_phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs");

            migrationBuilder.DropTable(
                name: "group_mentor_history");

            migrationBuilder.AlterColumn<Guid>(
                name: "internship_phase_id",
                table: "jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id1",
                table: "internship_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_job_id1",
                table: "internship_applications",
                column: "job_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_internship_applications_jobs_job_id1",
                table: "internship_applications",
                column: "job_id1",
                principalTable: "jobs",
                principalColumn: "job_id");

            migrationBuilder.AddForeignKey(
                name: "fk_jobs_internship_phases_internship_phase_id",
                table: "jobs",
                column: "internship_phase_id",
                principalTable: "internship_phases",
                principalColumn: "phase_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
