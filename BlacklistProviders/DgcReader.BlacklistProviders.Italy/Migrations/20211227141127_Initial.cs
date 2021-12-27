using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DgcReader.BlacklistProviders.Italy.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DgcReader_ItalianDrl_Blacklist",
                columns: table => new
                {
                    HashedUCVI = table.Column<string>(maxLength: 44, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DgcReader_ItalianDrl_Blacklist", x => x.HashedUCVI);
                });

            migrationBuilder.CreateTable(
                name: "DgcReader_ItalianDrl_SyncStatus",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    ChunksCount = table.Column<int>(nullable: false),
                    LastCheck = table.Column<DateTime>(nullable: false),
                    LastChunkSaved = table.Column<int>(nullable: false),
                    LocalVersion = table.Column<int>(nullable: false),
                    LocalVersionId = table.Column<string>(maxLength: 24, nullable: true),
                    TargetVersion = table.Column<int>(nullable: false),
                    TargetVersionId = table.Column<string>(maxLength: 24, nullable: true),
                    TotalNumberUCVI = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DgcReader_ItalianDrl_SyncStatus", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DgcReader_ItalianDrl_Blacklist");

            migrationBuilder.DropTable(
                name: "DgcReader_ItalianDrl_SyncStatus");
        }
    }
}
