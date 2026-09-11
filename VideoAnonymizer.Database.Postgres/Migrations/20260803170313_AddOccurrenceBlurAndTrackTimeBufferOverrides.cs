using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrenceBlurAndTrackTimeBufferOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccurrenceBlurSizePercentOverride",
                table: "DetectedObjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrackTimeBufferMsOverride",
                table: "DetectedObjects",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccurrenceBlurSizePercentOverride",
                table: "DetectedObjects");

            migrationBuilder.DropColumn(
                name: "TrackTimeBufferMsOverride",
                table: "DetectedObjects");
        }
    }
}
