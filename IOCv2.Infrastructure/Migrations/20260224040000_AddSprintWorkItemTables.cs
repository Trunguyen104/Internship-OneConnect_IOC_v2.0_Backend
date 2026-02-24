using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintWorkItemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_items",
                columns: table => new
                {
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<short>(type: "smallint", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    story_point = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    backlog_order = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    original_estimate = table.Column<float>(type: "real", nullable: true),
                    remaining_work = table.Column<float>(type: "real", nullable: true),
                    assignee_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_items", x => x.work_item_id);
                    table.ForeignKey(
                        name: "fk_work_items_work_items_parent_id",
                        column: x => x.parent_id,
                        principalTable: "work_items",
                        principalColumn: "work_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    goal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprints", x => x.sprint_id);
                });

            migrationBuilder.CreateTable(
                name: "sprint_work_items",
                columns: table => new
                {
                    sprint_work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_work_items", x => x.sprint_work_item_id);
                    table.ForeignKey(
                        name: "fk_sprint_work_items_sprints_sprint_id",
                        column: x => x.sprint_id,
                        principalTable: "sprints",
                        principalColumn: "sprint_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sprint_work_items_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "work_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_items_backlog_order",
                table: "work_items",
                column: "backlog_order");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_parent_id",
                table: "work_items",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_project_id",
                table: "work_items",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_status",
                table: "work_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_type",
                table: "work_items",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_sprints_project_id",
                table: "sprints",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprints_status",
                table: "sprints",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sprint_work_items_board_order",
                table: "sprint_work_items",
                columns: new[] { "sprint_id", "board_order" });

            migrationBuilder.CreateIndex(
                name: "ix_sprint_work_items_unique",
                table: "sprint_work_items",
                columns: new[] { "sprint_id", "work_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sprint_work_items_work_item_id",
                table: "sprint_work_items",
                column: "work_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sprint_work_items");
            migrationBuilder.DropTable(name: "sprints");
            migrationBuilder.DropTable(name: "work_items");
        }
    }
}
