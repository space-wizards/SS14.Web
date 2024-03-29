﻿// <auto-generated />
using System;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SS14.ServerHub.Shared.Data;

#nullable disable

namespace SS14.ServerHub.Shared.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20230622181526_UniqueServerNameTimes")]
    partial class UniqueServerNameTimes
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.AdvertisedServer", b =>
                {
                    b.Property<int>("AdvertisedServerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("AdvertisedServerId"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<IPAddress>("AdvertiserAddress")
                        .HasColumnType("inet");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("timestamp with time zone");

                    b.Property<byte[]>("InfoData")
                        .HasColumnType("jsonb");

                    b.Property<byte[]>("StatusData")
                        .HasColumnType("jsonb");

                    b.HasKey("AdvertisedServerId");

                    b.HasIndex("Address")
                        .IsUnique();

                    b.ToTable("AdvertisedServer");

                    b.HasCheckConstraint("AddressSs14Uri", "\"Address\" LIKE 'ss14://%' OR \"Address\" LIKE 'ss14s://%'");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.BannedAddress", b =>
                {
                    b.Property<int>("BannedAddressId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BannedAddressId"));

                    b.Property<ValueTuple<IPAddress, int>>("Address")
                        .HasColumnType("inet");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("BannedAddressId");

                    b.ToTable("BannedAddress");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.BannedDomain", b =>
                {
                    b.Property<int>("BannedDomainId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BannedDomainId"));

                    b.Property<string>("DomainName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("BannedDomainId");

                    b.ToTable("BannedDomain");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.ServerStatusArchive", b =>
                {
                    b.Property<int>("AdvertisedServerId")
                        .HasColumnType("integer");

                    b.Property<int>("ServerStatusArchiveId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ServerStatusArchiveId"));

                    b.Property<IPAddress>("AdvertiserAddress")
                        .HasColumnType("inet");

                    b.Property<byte[]>("StatusData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<DateTime>("Time")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("AdvertisedServerId", "ServerStatusArchiveId");

                    b.ToTable("ServerStatusArchive");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.UniqueServerName", b =>
                {
                    b.Property<int>("AdvertisedServerId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("FirstSeen")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastSeen")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("AdvertisedServerId", "Name");

                    b.ToTable("UniqueServerName");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.ServerStatusArchive", b =>
                {
                    b.HasOne("SS14.ServerHub.Shared.Data.AdvertisedServer", "AdvertisedServer")
                        .WithMany()
                        .HasForeignKey("AdvertisedServerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdvertisedServer");
                });

            modelBuilder.Entity("SS14.ServerHub.Shared.Data.UniqueServerName", b =>
                {
                    b.HasOne("SS14.ServerHub.Shared.Data.AdvertisedServer", "AdvertisedServer")
                        .WithMany()
                        .HasForeignKey("AdvertisedServerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdvertisedServer");
                });
#pragma warning restore 612, 618
        }
    }
}
