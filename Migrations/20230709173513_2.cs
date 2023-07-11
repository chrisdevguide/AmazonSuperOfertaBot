using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ElAhorrador.Migrations
{
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmazonProducts");

            migrationBuilder.CreateTable(
                name: "TelegramChats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatCommand = table.Column<string>(type: "text", nullable: true),
                    ChatStep = table.Column<int>(type: "integer", nullable: false),
                    StartedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramChats", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramChats");

            migrationBuilder.CreateTable(
                name: "AmazonProducts",
                columns: table => new
                {
                    Asin = table.Column<string>(type: "text", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    HasStock = table.Column<bool>(type: "boolean", nullable: false),
                    InitialScrapedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastScrapedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousPrices = table.Column<List<decimal>>(type: "numeric[]", nullable: true),
                    ReviewsCount = table.Column<int>(type: "integer", nullable: false),
                    Stars = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmazonProducts", x => x.Asin);
                });
        }
    }
}
