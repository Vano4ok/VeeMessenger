﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VeeMessenger.Data.Context;

#nullable disable

namespace VeeMessenger.Data.Migrations
{
    [DbContext(typeof(VeeMessengerContext))]
    partial class VeeMessengerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("VeeMessenger.Data.Entities.RefreshSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime2");

                    b.Property<string>("FingerPrint")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("RefreshSessions", (string)null);
                });

            modelBuilder.Entity("VeeMessenger.Data.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Bio")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("ImageId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LockOutDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("VeeMessenger.Data.Entities.UserTempData", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("EmailConfirmCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NumbersOfAttempts")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("UsersTempData", (string)null);
                });

            modelBuilder.Entity("VeeMessenger.Data.Entities.RefreshSession", b =>
                {
                    b.HasOne("VeeMessenger.Data.Entities.User", "User")
                        .WithMany("RefreshSessions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("VeeMessenger.Data.Entities.UserTempData", b =>
                {
                    b.HasOne("VeeMessenger.Data.Entities.User", "User")
                        .WithOne("UserTempData")
                        .HasForeignKey("VeeMessenger.Data.Entities.UserTempData", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("VeeMessenger.Data.Entities.User", b =>
                {
                    b.Navigation("RefreshSessions");

                    b.Navigation("UserTempData")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
