using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmazonSuperOfertaBot.Migrations
{
    public partial class _7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmazonProductsTelegram",
                columns: table => new
                {
                    Asin = table.Column<string>(type: "text", nullable: false),
                    LastPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    LastSearchedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentToTelegram = table.Column<bool>(type: "boolean", nullable: false),
                    LastSentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmazonProductsTelegram", x => x.Asin);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmazonProductsTelegram");
        }
    }
}
