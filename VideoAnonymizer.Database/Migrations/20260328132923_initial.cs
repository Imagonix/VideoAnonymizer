using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoAnonymizer.Database.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePath = table.Column<string>(type: "text", nullable: false),
                    AnonomizedPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzedFrames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeSeconds = table.Column<double>(type: "double precision", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzedFrames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyzedFrames_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectedObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    ClassName = table.Column<string>(type: "text", nullable: true),
                    Selected = table.Column<bool>(type: "boolean", nullable: false),
                    TrackId = table.Column<int>(type: "integer", nullable: true),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    AnalyzedFrameId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedObjects_AnalyzedFrames_AnalyzedFrameId",
                        column: x => x.AnalyzedFrameId,
                        principalTable: "AnalyzedFrames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedFrames_VideoId",
                table: "AnalyzedFrames",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedObjects_AnalyzedFrameId",
                table: "DetectedObjects",
                column: "AnalyzedFrameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedObjects");

            migrationBuilder.DropTable(
                name: "AnalyzedFrames");

            migrationBuilder.DropTable(
                name: "Videos");
        }
    }
}
