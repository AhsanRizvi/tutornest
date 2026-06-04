using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorNest.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Scaling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredById",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MeetingLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveClasses_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveClasses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: true),
                    CertificateCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Certificates_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Certificates_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CourseId",
                table: "Classes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferredById",
                table: "AspNetUsers",
                column: "ReferredById");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateCode",
                table: "Certificates",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ClassId",
                table: "Certificates",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CourseId",
                table: "Certificates",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId",
                table: "Courses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveClasses_ClassId",
                table: "LiveClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveClasses_TeacherId",
                table: "LiveClasses",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ReferredById",
                table: "AspNetUsers",
                column: "ReferredById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_ReferredById",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "LiveClasses");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Classes_CourseId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReferredById",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReferredById",
                table: "AspNetUsers");
        }
    }
}
