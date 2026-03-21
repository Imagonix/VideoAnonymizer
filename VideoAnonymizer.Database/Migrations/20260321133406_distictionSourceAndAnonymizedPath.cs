using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Migrations
{
    /// <inheritdoc />
    public partial class distictionSourceAndAnonymizedPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Videos",
                newName: "SourcePath");

            migrationBuilder.AddColumn<string>(
                name: "AnonomizedPath",
                table: "Videos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonomizedPath",
                table: "Videos");

            migrationBuilder.RenameColumn(
                name: "SourcePath",
                table: "Videos",
                newName: "Path");
        }
    }
}
