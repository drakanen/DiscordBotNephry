﻿// <auto-generated />
using DiscordBot.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DiscordBot.Migrations
{
    [DbContext(typeof(SqliteDbContext))]
    [Migration("20190531145702_Migrations")]
    partial class Migrations
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity("DiscordBot.Resources.Database.BannedWords", b =>
                {
                    b.Property<int>("UniqueNumber")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("Serverid");

                    b.Property<string>("Word");

                    b.HasKey("UniqueNumber");

                    b.ToTable("BannedWords");
                });

            modelBuilder.Entity("DiscordBot.Resources.Database.CustomCommands", b =>
                {
                    b.Property<int>("UniqueNumber")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Command");

                    b.Property<string>("CommandDescription");

                    b.Property<string>("CommandName");

                    b.Property<string>("Destination");

                    b.Property<ulong>("Serverid");

                    b.HasKey("UniqueNumber");

                    b.ToTable("CustomCommands");
                });

            modelBuilder.Entity("DiscordBot.Resources.Database.Gold", b =>
                {
                    b.Property<int>("UniqueNumber")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Amount");

                    b.Property<ulong>("Serverid");

                    b.Property<ulong>("UserId");

                    b.Property<string>("Username");

                    b.HasKey("UniqueNumber");

                    b.ToTable("Gold");
                });

            modelBuilder.Entity("DiscordBot.Resources.Database.GuildLocationSettings", b =>
                {
                    b.Property<ulong>("Serverid")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("BotSpamChannel");

                    b.Property<ulong>("ChatLogChannel");

                    b.Property<int>("GoldInterest");

                    b.Property<ulong>("ModLogChannel");

                    b.Property<string>("ServerName");

                    b.Property<ulong>("WelcomeChannel");

                    b.Property<string>("WelcomeMessage");

                    b.HasKey("Serverid");

                    b.ToTable("GuildLocationSettings");
                });

            modelBuilder.Entity("DiscordBot.Resources.Database.Warning", b =>
                {
                    b.Property<int>("UniqueNumber")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AmountOfWarnings");

                    b.Property<ulong>("Serverid");

                    b.Property<ulong>("UserId");

                    b.Property<string>("Username");

                    b.HasKey("UniqueNumber");

                    b.ToTable("Warnings");
                });
#pragma warning restore 612, 618
        }
    }
}
