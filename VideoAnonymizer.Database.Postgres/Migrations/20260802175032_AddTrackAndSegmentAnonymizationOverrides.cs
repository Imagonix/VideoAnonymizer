using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Postgres.Migrations
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
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostBufferMsOverride",
                table: "DetectedObjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreBufferMsOverride",
                table: "DetectedObjects",
                type: "integer",
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
