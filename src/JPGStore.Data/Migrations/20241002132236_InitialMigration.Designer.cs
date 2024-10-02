﻿// <auto-generated />
using System.Text.Json;
using JPGStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JPGStore.Data.Migrations
{
    [DbContext(typeof(JPGStoreSyncDbContext))]
    [Migration("20241002132236_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("public")
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Cardano.Sync.Data.Models.Block", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("Number")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id", "Number", "Slot");

                    b.HasIndex("Slot");

                    b.ToTable("Blocks", "public");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.ReducerState", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Name");

                    b.ToTable("ReducerStates", "public");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.TransactionOutput", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<long>("Index")
                        .HasColumnType("bigint");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id", "Index");

                    b.HasIndex("Slot");

                    b.ToTable("TransactionOutputs", "public");
                });

            modelBuilder.Entity("JPGStore.Data.Models.Reducers.ListingByAddress", b =>
                {
                    b.Property<string>("OwnerAddress")
                        .HasColumnType("text");

                    b.Property<string>("TxHash")
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TxIndex")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("UtxoStatus")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("BuyerAddress")
                        .HasColumnType("text");

                    b.Property<decimal>("EstimatedFromListingValue")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("EstimatedToListingValue")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("EstimatedTotalListingValue")
                        .HasColumnType("numeric(20,0)");

                    b.Property<byte[]>("ListingDatumCbor")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<string>("SpentTxHash")
                        .HasColumnType("text");

                    b.HasKey("OwnerAddress", "TxHash", "Slot", "TxIndex", "UtxoStatus", "Status");

                    b.HasIndex("EstimatedFromListingValue");

                    b.HasIndex("EstimatedTotalListingValue");

                    b.HasIndex("OwnerAddress");

                    b.HasIndex("Slot");

                    b.HasIndex("Status");

                    b.HasIndex("TxHash");

                    b.HasIndex("TxIndex");

                    b.HasIndex("UtxoStatus");

                    b.ToTable("ListingsByAddress", "public");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.TransactionOutput", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Datum", "Datum", b1 =>
                        {
                            b1.Property<string>("TransactionOutputId")
                                .HasColumnType("text");

                            b1.Property<long>("TransactionOutputIndex")
                                .HasColumnType("bigint");

                            b1.Property<byte[]>("Data")
                                .IsRequired()
                                .HasColumnType("bytea");

                            b1.Property<int>("Type")
                                .HasColumnType("integer");

                            b1.HasKey("TransactionOutputId", "TransactionOutputIndex");

                            b1.ToTable("TransactionOutputs", "public");

                            b1.WithOwner()
                                .HasForeignKey("TransactionOutputId", "TransactionOutputIndex");
                        });

                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("TransactionOutputId")
                                .HasColumnType("text");

                            b1.Property<long>("TransactionOutputIndex")
                                .HasColumnType("bigint");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("TransactionOutputId", "TransactionOutputIndex");

                            b1.ToTable("TransactionOutputs", "public");

                            b1.WithOwner()
                                .HasForeignKey("TransactionOutputId", "TransactionOutputIndex");
                        });

                    b.Navigation("Amount")
                        .IsRequired();

                    b.Navigation("Datum");
                });

            modelBuilder.Entity("JPGStore.Data.Models.Reducers.ListingByAddress", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("ListingByAddressOwnerAddress")
                                .HasColumnType("text");

                            b1.Property<string>("ListingByAddressTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("ListingByAddressSlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("ListingByAddressTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<int>("ListingByAddressUtxoStatus")
                                .HasColumnType("integer");

                            b1.Property<int>("ListingByAddressStatus")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("ListingByAddressOwnerAddress", "ListingByAddressTxHash", "ListingByAddressSlot", "ListingByAddressTxIndex", "ListingByAddressUtxoStatus", "ListingByAddressStatus");

                            b1.ToTable("ListingsByAddress", "public");

                            b1.WithOwner()
                                .HasForeignKey("ListingByAddressOwnerAddress", "ListingByAddressTxHash", "ListingByAddressSlot", "ListingByAddressTxIndex", "ListingByAddressUtxoStatus", "ListingByAddressStatus");
                        });

                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "SellerPayoutValue", b1 =>
                        {
                            b1.Property<string>("ListingByAddressOwnerAddress")
                                .HasColumnType("text");

                            b1.Property<string>("ListingByAddressTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("ListingByAddressSlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("ListingByAddressTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<int>("ListingByAddressUtxoStatus")
                                .HasColumnType("integer");

                            b1.Property<int>("ListingByAddressStatus")
                                .HasColumnType("integer");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("ListingByAddressOwnerAddress", "ListingByAddressTxHash", "ListingByAddressSlot", "ListingByAddressTxIndex", "ListingByAddressUtxoStatus", "ListingByAddressStatus");

                            b1.ToTable("ListingsByAddress", "public");

                            b1.WithOwner()
                                .HasForeignKey("ListingByAddressOwnerAddress", "ListingByAddressTxHash", "ListingByAddressSlot", "ListingByAddressTxIndex", "ListingByAddressUtxoStatus", "ListingByAddressStatus");
                        });

                    b.Navigation("Amount")
                        .IsRequired();

                    b.Navigation("SellerPayoutValue");
                });
#pragma warning restore 612, 618
        }
    }
}
