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
                    CurrentVersion = table.Column<int>(nullable: false),
                    CurrentVersionId = table.Column<string>(maxLength: 24, nullable: true),
                    LastCheck = table.Column<DateTime>(nullable: false),
                    LastChunkSaved = table.Column<int>(nullable: false),
                    TargetChunkSize = table.Column<int>(nullable: false),
                    TargetChunksCount = table.Column<int>(nullable: false),
                    TargetTotalNumberUCVI = table.Column<int>(nullable: false),
                    TargetVersion = table.Column<int>(nullable: false),
                    TargetVersionId = table.Column<string>(maxLength: 24, nullable: true)
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
