﻿// <auto-generated />
using System;
using System.Collections.Generic;
using AmazonApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmazonSuperOfertaBot.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230720172204_6")]
    partial class _6
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.19")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AmazonSuperOfertaBot.Models.AmazonCategory", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("AmazonCategories");
                });

            modelBuilder.Entity("AmazonSuperOfertaBot.Models.Log", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Data")
                        .HasColumnType("text");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("ElAhorrador.Models.AmazonAlert", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<List<decimal>>("Prices")
                        .HasColumnType("numeric[]");

                    b.Property<string>("ProductAsin")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("AmazonAlerts");
                });

            modelBuilder.Entity("ElAhorrador.Models.Configuration", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("Name");

                    b.ToTable("Configurations");
                });

            modelBuilder.Entity("ElAhorrador.Models.TelegramChat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("ChatCommand")
                        .HasColumnType("text");

                    b.Property<int>("ChatStep")
                        .HasColumnType("integer");

                    b.Property<string>("Data")
                        .HasColumnType("text");

                    b.Property<DateTime>("StartedTime")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("TelegramChats");
                });
#pragma warning restore 612, 618
        }
    }
}
