using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorNest.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoUrlToCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Certificates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Certificates");
        }
    }
}
