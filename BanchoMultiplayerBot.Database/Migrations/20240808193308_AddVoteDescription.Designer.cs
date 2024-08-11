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
    [Migration("20240808193308_AddVoteDescription")]
    partial class AddVoteDescription
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("BeatmapId")
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

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.LobbyBehaviorData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BehaviorName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("LobbyConfigurationId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("LobbyBehaviorData");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.LobbyConfiguration", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Behaviours")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Mode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Mods")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ScoreMode")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("TeamMode")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("LobbyConfigurations");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.LobbyRoomInstance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Channel")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("LobbyConfigurationId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("LobbyRoomInstances");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.LobbyTimer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LobbyId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LobbyTimers");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.LobbyVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LobbyId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Votes")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LobbyVotes");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.Map", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<float?>("AverageLeavePercentage")
                        .HasColumnType("REAL");

                    b.Property<float?>("AveragePassPercentage")
                        .HasColumnType("REAL");

                    b.Property<string>("BeatmapArtist")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<long>("BeatmapId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BeatmapName")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<long>("BeatmapSetId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DifficultyName")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastPlayed")
                        .HasColumnType("TEXT");

                    b.Property<float?>("StarRating")
                        .HasColumnType("REAL");

                    b.Property<int>("TimesPlayed")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Maps");
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

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("PlayerBans");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.Score", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("BeatmapId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Count100")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Count300")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Count50")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CountMiss")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GameId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LobbyId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Mods")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("OsuScoreId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("PlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Rank")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.Property<long>("TotalScore")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("UserId");

                    b.ToTable("Scores");
                });

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

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

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.Score", b =>
                {
                    b.HasOne("BanchoMultiplayerBot.Database.Models.Game", "Game")
                        .WithMany()
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BanchoMultiplayerBot.Database.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Game");

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
