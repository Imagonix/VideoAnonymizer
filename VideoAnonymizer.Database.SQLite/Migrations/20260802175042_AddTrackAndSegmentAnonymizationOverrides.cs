using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackAndSegmentAnonymizationOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlurSizePercentOverride",
                table: "DetectedObjects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostBufferMsOverride",
                table: "DetectedObjects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreBufferMsOverride",
                table: "DetectedObjects",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlurSizePercentOverride",
                table: "DetectedObjects");

            migrationBuilder.DropColumn(
                name: "PostBufferMsOverride",
                table: "DetectedObjects");

            migrationBuilder.DropColumn(
                name: "PreBufferMsOverride",
                table: "DetectedObjects");
        }
    }
}
