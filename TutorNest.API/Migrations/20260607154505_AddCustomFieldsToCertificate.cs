using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorNest.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsToCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomMessage",
                table: "Certificates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomSubTitle",
                table: "Certificates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomTitle",
                table: "Certificates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomMessage",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CustomSubTitle",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CustomTitle",
                table: "Certificates");
        }
    }
}
