using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmazonSuperOfertaBot.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmazonAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    ProductAsin = table.Column<string>(type: "text", nullable: true),
                    Prices = table.Column<List<decimal>>(type: "numeric[]", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmazonAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "TelegramChats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ChatCommand = table.Column<string>(type: "text", nullable: true),
                    ChatStep = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true),
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
                name: "AmazonAlerts");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "TelegramChats");
        }
    }
}
