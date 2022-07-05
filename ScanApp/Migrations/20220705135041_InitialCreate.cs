using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ScanApp.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanResults",
                columns: table => new
                {
                    ScanResultId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(nullable: true),
                    MD5 = table.Column<string>(nullable: true),
                    Sha1 = table.Column<string>(nullable: true),
                    Sha256 = table.Column<string>(nullable: true),
                    FileSize = table.Column<long>(nullable: false),
                    IsError = table.Column<bool>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true),
                    LastSeen = table.Column<DateTime>(nullable: true),
                    Scanned = table.Column<int>(nullable: false),
                    CacheKey = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanResults", x => x.ScanResultId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_CacheKey",
                table: "ScanResults",
                column: "CacheKey");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_MD5",
                table: "ScanResults",
                column: "MD5");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_Sha1",
                table: "ScanResults",
                column: "Sha1");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_Sha256",
                table: "ScanResults",
                column: "Sha256",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanResults");
        }
    }
}
