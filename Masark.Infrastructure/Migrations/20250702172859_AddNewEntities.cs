using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AdminUser_AdminUserId",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminUser",
                table: "AdminUser");

            migrationBuilder.RenameTable(
                name: "AdminUser",
                newName: "AdminUsers");

            migrationBuilder.AddColumn<string>(
                name: "OptionATextEs",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionATextZh",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionBTextEs",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionBTextZh",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextEs",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextZh",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualSalary",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OnetId",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnetJobZone",
                table: "Careers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OutlookGrowthPercentage",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillsRequired",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkContext",
                table: "Careers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentRating",
                table: "AssessmentSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentState",
                table: "AssessmentSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTieBreaker",
                table: "AssessmentSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminUsers",
                table: "AdminUsers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CareerClusterRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    DescriptionEn = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DescriptionAr = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerClusterRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentElementId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssessmentSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TitleAr = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    ContentAr = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsInteractive = table.Column<bool>(type: "INTEGER", nullable: false),
                    GraphData = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    ActivityData = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportElements_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportElements_ReportElements_ParentElementId",
                        column: x => x.ParentElementId,
                        principalTable: "ReportElements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TieBreakerQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TextEn = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TextAr = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    OptionAEn = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OptionAAr = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OptionBEn = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OptionBAr = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Dimension = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionAMapsToFirst = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TieBreakerQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerClusterUserRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssessmentSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CareerClusterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CareerClusterRatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    RatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerClusterUserRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerClusterUserRatings_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareerClusterUserRatings_CareerClusterRatings_CareerClusterRatingId",
                        column: x => x.CareerClusterRatingId,
                        principalTable: "CareerClusterRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareerClusterUserRatings_CareerClusters_CareerClusterId",
                        column: x => x.CareerClusterId,
                        principalTable: "CareerClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportElementQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportElementId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionTextEn = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    QuestionTextAr = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    QuestionType = table.Column<string>(type: "TEXT", nullable: false),
                    OptionsJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportElementQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportElementQuestions_ReportElements_ReportElementId",
                        column: x => x.ReportElementId,
                        principalTable: "ReportElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportElementRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportElementId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssessmentSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReportElementId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportElementRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportElementRatings_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportElementRatings_ReportElements_ReportElementId",
                        column: x => x.ReportElementId,
                        principalTable: "ReportElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportElementRatings_ReportElements_ReportElementId1",
                        column: x => x.ReportElementId1,
                        principalTable: "ReportElements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportUserAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportElementQuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssessmentSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnswerText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    AnswerRating = table.Column<int>(type: "INTEGER", nullable: true),
                    AnswerBoolean = table.Column<bool>(type: "INTEGER", nullable: true),
                    AnswerChoice = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReportElementQuestionId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportUserAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportUserAnswers_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportUserAnswers_ReportElementQuestions_ReportElementQuestionId",
                        column: x => x.ReportElementQuestionId,
                        principalTable: "ReportElementQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportUserAnswers_ReportElementQuestions_ReportElementQuestionId1",
                        column: x => x.ReportElementQuestionId1,
                        principalTable: "ReportElementQuestions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_TenantId_Email",
                table: "AdminUsers",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_TenantId_Username",
                table: "AdminUsers",
                columns: new[] { "TenantId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerClusterRatings_TenantId_Value",
                table: "CareerClusterRatings",
                columns: new[] { "TenantId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerClusterUserRatings_AssessmentSessionId",
                table: "CareerClusterUserRatings",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerClusterUserRatings_CareerClusterId",
                table: "CareerClusterUserRatings",
                column: "CareerClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerClusterUserRatings_CareerClusterRatingId",
                table: "CareerClusterUserRatings",
                column: "CareerClusterRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerClusterUserRatings_TenantId_AssessmentSessionId_CareerClusterRatingId",
                table: "CareerClusterUserRatings",
                columns: new[] { "TenantId", "AssessmentSessionId", "CareerClusterRatingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementQuestions_ReportElementId",
                table: "ReportElementQuestions",
                column: "ReportElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementQuestions_TenantId_ReportElementId_OrderIndex",
                table: "ReportElementQuestions",
                columns: new[] { "TenantId", "ReportElementId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementRatings_AssessmentSessionId",
                table: "ReportElementRatings",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementRatings_ReportElementId",
                table: "ReportElementRatings",
                column: "ReportElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementRatings_ReportElementId1",
                table: "ReportElementRatings",
                column: "ReportElementId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElementRatings_TenantId_AssessmentSessionId_ReportElementId",
                table: "ReportElementRatings",
                columns: new[] { "TenantId", "AssessmentSessionId", "ReportElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportElements_AssessmentSessionId",
                table: "ReportElements",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElements_ParentElementId",
                table: "ReportElements",
                column: "ParentElementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportElements_TenantId_AssessmentSessionId_OrderIndex",
                table: "ReportElements",
                columns: new[] { "TenantId", "AssessmentSessionId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportUserAnswers_AssessmentSessionId",
                table: "ReportUserAnswers",
                column: "AssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUserAnswers_ReportElementQuestionId",
                table: "ReportUserAnswers",
                column: "ReportElementQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUserAnswers_ReportElementQuestionId1",
                table: "ReportUserAnswers",
                column: "ReportElementQuestionId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUserAnswers_TenantId_AssessmentSessionId_ReportElementQuestionId",
                table: "ReportUserAnswers",
                columns: new[] { "TenantId", "AssessmentSessionId", "ReportElementQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TieBreakerQuestions_TenantId_Dimension_OrderIndex",
                table: "TieBreakerQuestions",
                columns: new[] { "TenantId", "Dimension", "OrderIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AdminUsers_AdminUserId",
                table: "AuditLogs",
                column: "AdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AdminUsers_AdminUserId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CareerClusterUserRatings");

            migrationBuilder.DropTable(
                name: "ReportElementRatings");

            migrationBuilder.DropTable(
                name: "ReportUserAnswers");

            migrationBuilder.DropTable(
                name: "TieBreakerQuestions");

            migrationBuilder.DropTable(
                name: "CareerClusterRatings");

            migrationBuilder.DropTable(
                name: "ReportElementQuestions");

            migrationBuilder.DropTable(
                name: "ReportElements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminUsers",
                table: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_TenantId_Email",
                table: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_TenantId_Username",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "OptionATextEs",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OptionATextZh",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OptionBTextEs",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OptionBTextZh",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TextEs",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TextZh",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AnnualSalary",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "OnetId",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "OnetJobZone",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "OutlookGrowthPercentage",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "SkillsRequired",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "WorkContext",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "AssessmentRating",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "CurrentState",
                table: "AssessmentSessions");

            migrationBuilder.DropColumn(
                name: "RequiresTieBreaker",
                table: "AssessmentSessions");

            migrationBuilder.RenameTable(
                name: "AdminUsers",
                newName: "AdminUser");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminUser",
                table: "AdminUser",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AdminUser_AdminUserId",
                table: "AuditLogs",
                column: "AdminUserId",
                principalTable: "AdminUser",
                principalColumn: "Id");
        }
    }
}
