using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddNextGapHandlingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextGapHandlingMode",
                table: "DetectedObjects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextGapHandlingMode",
                table: "DetectedObjects");
        }
    }
}
