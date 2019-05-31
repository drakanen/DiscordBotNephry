using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordBot.Migrations
{
    public partial class Migrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannedWords",
                columns: table => new
                {
                    UniqueNumber = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Serverid = table.Column<ulong>(nullable: false),
                    Word = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedWords", x => x.UniqueNumber);
                });

            migrationBuilder.CreateTable(
                name: "CustomCommands",
                columns: table => new
                {
                    UniqueNumber = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Serverid = table.Column<ulong>(nullable: false),
                    Destination = table.Column<string>(nullable: true),
                    CommandName = table.Column<string>(nullable: true),
                    Command = table.Column<string>(nullable: true),
                    CommandDescription = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommands", x => x.UniqueNumber);
                });

            migrationBuilder.CreateTable(
                name: "Gold",
                columns: table => new
                {
                    UniqueNumber = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Serverid = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    Amount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gold", x => x.UniqueNumber);
                });

            migrationBuilder.CreateTable(
                name: "GuildLocationSettings",
                columns: table => new
                {
                    Serverid = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>(nullable: true),
                    WelcomeChannel = table.Column<ulong>(nullable: false),
                    WelcomeMessage = table.Column<string>(nullable: true),
                    BotSpamChannel = table.Column<ulong>(nullable: false),
                    ChatLogChannel = table.Column<ulong>(nullable: false),
                    ModLogChannel = table.Column<ulong>(nullable: false),
                    GoldInterest = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildLocationSettings", x => x.Serverid);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    UniqueNumber = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Serverid = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    AmountOfWarnings = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.UniqueNumber);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedWords");

            migrationBuilder.DropTable(
                name: "CustomCommands");

            migrationBuilder.DropTable(
                name: "Gold");

            migrationBuilder.DropTable(
                name: "GuildLocationSettings");

            migrationBuilder.DropTable(
                name: "Warnings");
        }
    }
}
