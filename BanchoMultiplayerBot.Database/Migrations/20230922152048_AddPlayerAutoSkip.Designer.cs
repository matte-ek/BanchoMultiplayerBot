﻿// <auto-generated />
using System;
using BanchoMultiplayerBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BanchoMultiplayerBot.Database.Bot.Migrations
{
    [DbContext(typeof(BotDbContext))]
    [Migration("20230922152048_AddPlayerAutoSkip")]
    partial class AddPlayerAutoSkip
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BeatmapId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerFinishCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerPassedCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.MapBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("BeatmapId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("BeatmapSetId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("MapBans");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.PlayerBan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("Expire")
                        .HasColumnType("TEXT");

                    b.Property<bool>("HostBan")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Reason")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("PlayerBans");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Administrator")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AutoSkipEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchesPlayed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("NumberOneResults")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Playtime")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.PlayerBan", b =>
                {
                    b.HasOne("BanchoMultiplayerBot.Database.Models.User", "User")
                        .WithMany("Bans")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.User", b =>
                {
                    b.Navigation("Bans");
                });
#pragma warning restore 612, 618
        }
    }
}
