using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImageDescriptionApp.Migrations
{
    /// <inheritdoc />
    public partial class AddImageDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Images");

            migrationBuilder.AddColumn<string>(
                name: "AzureDescription",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClarifaiColors",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClarifaiTags",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroqDescription",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserMessage",
                table: "Images",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AzureDescription",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ClarifaiColors",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ClarifaiTags",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "GroqDescription",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "UserMessage",
                table: "Images");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Images",
                type: "TEXT",
                nullable: true);
        }
    }
}
