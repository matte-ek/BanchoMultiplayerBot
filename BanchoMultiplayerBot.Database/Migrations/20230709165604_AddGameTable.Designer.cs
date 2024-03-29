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
    [Migration("20230709165604_AddGameTable")]
    partial class AddGameTable
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

            modelBuilder.Entity("BanchoMultiplayerBot.Database.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

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
#pragma warning restore 612, 618
        }
    }
}
