using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTrackTimeBufferOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackTimeBufferMsOverride",
                table: "DetectedObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrackTimeBufferMsOverride",
                table: "DetectedObjects",
                type: "integer",
                nullable: true);
        }
    }
}
