using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOCv2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "enterprises",
                columns: table => new
                {
                    enterprise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    industry = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    background_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enterprises", x => x.enterprise_id);
                });

            migrationBuilder.CreateTable(
                name: "universities",
                columns: table => new
                {
                    uni_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_universities", x => x.uni_id);
                });

            migrationBuilder.CreateTable(
                name: "user_code_sequences",
                columns: table => new
                {
                    role = table.Column<short>(type: "smallint", nullable: false),
                    current_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_code_sequences", x => x.role);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    role = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "terms",
                columns: table => new
                {
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    university_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    total_enrolled = table.Column<int>(type: "integer", nullable: false),
                    total_placed = table.Column<int>(type: "integer", nullable: false),
                    total_unplaced = table.Column<int>(type: "integer", nullable: false),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    close_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms", x => x.term_id);
                    table.ForeignKey(
                        name: "fk_terms_universities_university_id",
                        column: x => x.university_id,
                        principalTable: "universities",
                        principalColumn: "uni_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<short>(type: "smallint", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_performed_by_id",
                        column: x => x.performed_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enterprise_users",
                columns: table => new
                {
                    enterprise_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enterprise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    expertise = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enterprise_users", x => x.enterprise_user_id);
                    table.ForeignKey(
                        name: "fk_enterprise_users_enterprises_enterprise_id",
                        column: x => x.enterprise_id,
                        principalTable: "enterprises",
                        principalColumn: "enterprise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_enterprise_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    token_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.token_id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    major = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gpa = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    highest_degree = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    portfolio_url = table.Column<string>(type: "text", nullable: true),
                    internship_status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.student_id);
                    table.ForeignKey(
                        name: "fk_students_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "university_users",
                columns: table => new
                {
                    uni_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uni_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "text", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_university_users", x => x.uni_user_id);
                    table.ForeignKey(
                        name: "fk_university_users_universities_university_id",
                        column: x => x.uni_id,
                        principalTable: "universities",
                        principalColumn: "uni_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_university_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_cycles",
                columns: table => new
                {
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluation_cycles", x => x.cycle_id);
                    table.ForeignKey(
                        name: "fk_evaluation_cycles_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "term_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "internship_groups",
                columns: table => new
                {
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    enterprise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internship_groups", x => x.internship_id);
                    table.ForeignKey(
                        name: "fk_internship_groups_enterprise_users_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "enterprise_users",
                        principalColumn: "enterprise_user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_internship_groups_enterprises_enterprise_id",
                        column: x => x.enterprise_id,
                        principalTable: "enterprises",
                        principalColumn: "enterprise_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_internship_groups_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "term_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_terms",
                columns: table => new
                {
                    term_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_terms", x => new { x.student_id, x.term_id });
                    table.ForeignKey(
                        name: "fk_student_terms_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_student_terms_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "term_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_criteria",
                columns: table => new
                {
                    criteria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    max_score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluation_criteria", x => x.criteria_id);
                    table.ForeignKey(
                        name: "fk_evaluation_criteria_evaluation_cycles_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "evaluation_cycles",
                        principalColumn: "cycle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evaluator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluations", x => x.evaluation_id);
                    table.ForeignKey(
                        name: "fk_evaluations_evaluation_cycles_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "evaluation_cycles",
                        principalColumn: "cycle_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluations_users_evaluator_id",
                        column: x => x.evaluator_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "internship_applications",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    applied_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    reviewed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internship_applications", x => x.application_id);
                    table.ForeignKey(
                        name: "fk_internship_applications_enterprise_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "enterprise_users",
                        principalColumn: "enterprise_user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_internship_applications_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id");
                    table.ForeignKey(
                        name: "fk_internship_applications_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "internship_students",
                columns: table => new
                {
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internship_students", x => new { x.internship_id, x.student_id });
                    table.ForeignKey(
                        name: "fk_internship_students_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_internship_students_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logbooks",
                columns: table => new
                {
                    logbook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date_report = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    issue = table.Column<string>(type: "text", nullable: true),
                    plan = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logbooks", x => x.logbook_id);
                    table.ForeignKey(
                        name: "fk_logbooks_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_logbooks_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    internship_group_internship_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.project_id);
                    table.ForeignKey(
                        name: "fk_projects_internship_groups_internship_group_internship_id",
                        column: x => x.internship_group_internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id");
                    table.ForeignKey(
                        name: "fk_projects_internship_groups_internship_id",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stakeholders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    internship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stakeholders", x => x.id);
                    table.ForeignKey(
                        name: "fk_stakeholders_internship_groups",
                        column: x => x.internship_id,
                        principalTable: "internship_groups",
                        principalColumn: "internship_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_details",
                columns: table => new
                {
                    detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criteria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluation_details", x => x.detail_id);
                    table.ForeignKey(
                        name: "fk_evaluation_details_evaluation_criteria_criteria_id",
                        column: x => x.criteria_id,
                        principalTable: "evaluation_criteria",
                        principalColumn: "criteria_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evaluation_details_evaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "evaluations",
                        principalColumn: "evaluation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_resources",
                columns: table => new
                {
                    project_resource_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resource_type = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    resource_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'UTC'"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_resources", x => x.project_resource_id);
                    table.ForeignKey(
                        name: "fk_project_resources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    goal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprints", x => x.sprint_id);
                    table.ForeignKey(
                        name: "fk_sprints_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    story_point = table.Column<int>(type: "integer", nullable: true),
                    priority = table.Column<short>(type: "smallint", nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    backlog_order = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_items", x => x.work_item_id);
                    table.ForeignKey(
                        name: "fk_work_items_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_work_items_students_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_work_items_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "fk_work_items_work_items_parent_id",
                        column: x => x.parent_id,
                        principalTable: "work_items",
                        principalColumn: "work_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stakeholder_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    stakeholder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stakeholder_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_stakeholder_issues_stakeholders",
                        column: x => x.stakeholder_id,
                        principalTable: "stakeholders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logbook_work_items",
                columns: table => new
                {
                    logbook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_items_work_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logbook_work_items", x => new { x.logbook_id, x.work_items_work_item_id });
                    table.ForeignKey(
                        name: "fk_logbook_work_items_logbooks_logbook_id",
                        column: x => x.logbook_id,
                        principalTable: "logbooks",
                        principalColumn: "logbook_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_logbook_work_items_work_items_work_items_work_item_id",
                        column: x => x.work_items_work_item_id,
                        principalTable: "work_items",
                        principalColumn: "work_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprint_work_items",
                columns: table => new
                {
                    sprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_order = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sprint_work_items", x => new { x.sprint_id, x.work_item_id });
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
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_performed_by_id",
                table: "audit_logs",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "ix_enterprise_users_enterprise_id",
                table: "enterprise_users",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_enterprise_users_user_id",
                table: "enterprise_users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enterprises_name",
                table: "enterprises",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_enterprises_tax_code",
                table: "enterprises",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_criteria_cycle_id",
                table: "evaluation_criteria",
                column: "cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_cycles_status",
                table: "evaluation_cycles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_cycles_term_id",
                table: "evaluation_cycles",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_criteria_id",
                table: "evaluation_details",
                column: "criteria_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluation_details_evaluation_criteria_unique",
                table: "evaluation_details",
                columns: new[] { "evaluation_id", "criteria_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_cycle_internship_student_unique",
                table: "evaluations",
                columns: new[] { "cycle_id", "internship_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_evaluator_id",
                table: "evaluations",
                column: "evaluator_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_internship_id",
                table: "evaluations",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_student_id",
                table: "evaluations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_internship_id_student_id",
                table: "internship_applications",
                columns: new[] { "internship_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_reviewed_by",
                table: "internship_applications",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_internship_applications_student_id",
                table: "internship_applications",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_groups_enterprise_id",
                table: "internship_groups",
                column: "enterprise_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_groups_internship_id",
                table: "internship_groups",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_groups_mentor_id",
                table: "internship_groups",
                column: "mentor_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_groups_term_id",
                table: "internship_groups",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_internship_students_student_id",
                table: "internship_students",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_logbook_work_items_work_items_work_item_id",
                table: "logbook_work_items",
                column: "work_items_work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_logbooks_internship_id",
                table: "logbooks",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_logbooks_student_id",
                table: "logbooks",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_expires_at",
                table: "password_reset_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_token_hash",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_resources_created_at",
                table: "project_resources",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_project_resources_project_id",
                table: "project_resources",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_resources_resource_type",
                table: "project_resources",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_at",
                table: "projects",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_projects_internship_group_internship_id",
                table: "projects",
                column: "internship_group_internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_internship_id",
                table: "projects",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                table: "projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprint_work_items_board_order",
                table: "sprint_work_items",
                columns: new[] { "sprint_id", "board_order" });

            migrationBuilder.CreateIndex(
                name: "ix_sprint_work_items_work_item_id",
                table: "sprint_work_items",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprints_project_id",
                table: "sprints",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprints_status",
                table: "sprints",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholder_issues_stakeholder_id",
                table: "stakeholder_issues",
                column: "stakeholder_id");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholder_issues_status",
                table: "stakeholder_issues",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholders_email",
                table: "stakeholders",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholders_internship_email_unique",
                table: "stakeholders",
                columns: new[] { "internship_id", "email" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stakeholders_internship_id",
                table: "stakeholders",
                column: "internship_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_terms_term_id",
                table: "student_terms",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_user_id",
                table: "students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terms_university_id",
                table: "terms",
                column: "university_id");

            migrationBuilder.CreateIndex(
                name: "ix_universities_code",
                table: "universities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_university_users_university_id",
                table: "university_users",
                column: "uni_id");

            migrationBuilder.CreateIndex(
                name: "ix_university_users_user_id",
                table: "university_users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role_status",
                table: "users",
                columns: new[] { "role", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_items_assignee_id",
                table: "work_items",
                column: "assignee_id");

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
                name: "ix_work_items_student_id",
                table: "work_items",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_type",
                table: "work_items",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "evaluation_details");

            migrationBuilder.DropTable(
                name: "internship_applications");

            migrationBuilder.DropTable(
                name: "internship_students");

            migrationBuilder.DropTable(
                name: "logbook_work_items");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "project_resources");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sprint_work_items");

            migrationBuilder.DropTable(
                name: "stakeholder_issues");

            migrationBuilder.DropTable(
                name: "student_terms");

            migrationBuilder.DropTable(
                name: "university_users");

            migrationBuilder.DropTable(
                name: "user_code_sequences");

            migrationBuilder.DropTable(
                name: "evaluation_criteria");

            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "logbooks");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropTable(
                name: "work_items");

            migrationBuilder.DropTable(
                name: "stakeholders");

            migrationBuilder.DropTable(
                name: "evaluation_cycles");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "internship_groups");

            migrationBuilder.DropTable(
                name: "enterprise_users");

            migrationBuilder.DropTable(
                name: "terms");

            migrationBuilder.DropTable(
                name: "enterprises");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "universities");
        }
    }
}
