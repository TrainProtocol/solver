﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Train.Solver.Repositories.Npgsql;

#nullable disable

namespace Train.Solver.Data.EF.Migrations
{
    [DbContext(typeof(SolverDbContext))]
    partial class SolverDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Contract", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("NetworkId")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,EvmMultiCallContract=3");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("NetworkId");

                    b.ToTable("Contracts");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Expense", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("FeeTokenId")
                        .HasColumnType("integer");

                    b.Property<decimal[]>("LastFeeValues")
                        .IsRequired()
                        .HasColumnType("numeric[]");

                    b.Property<int>("TokenId")
                        .HasColumnType("integer");

                    b.Property<int>("TransactionType")
                        .HasColumnType("integer")
                        .HasComment("Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("FeeTokenId");

                    b.HasIndex("TokenId", "FeeTokenId", "TransactionType")
                        .IsUnique();

                    b.ToTable("Expenses");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.ManagedAccount", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("NetworkId")
                        .HasColumnType("integer");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("LP=0,Charging=1");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("Address");

                    b.HasIndex("NetworkId");

                    b.ToTable("ManagedAccounts");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Network", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AccountExplorerTemplate")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ChainId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("FeePercentageIncrease")
                        .HasColumnType("integer");

                    b.Property<int>("FeeType")
                        .HasColumnType("integer");

                    b.Property<string>("FixedGasPriceInGwei")
                        .HasColumnType("text");

                    b.Property<int?>("GasLimitPercentageIncrease")
                        .HasColumnType("integer");

                    b.Property<bool>("IsExternal")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsTestnet")
                        .HasColumnType("boolean");

                    b.Property<string>("Logo")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ReplacementFeePercentage")
                        .HasColumnType("integer");

                    b.Property<string>("TransactionExplorerTemplate")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("EVM=0,Solana=1,Starknet=2");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Networks");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Node", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("NetworkId")
                        .HasColumnType("integer");

                    b.Property<double>("Priority")
                        .HasColumnType("double precision");

                    b.Property<bool>("TraceEnabled")
                        .HasColumnType("boolean");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("Primary=0,DepositTracking=1,Public=2,Secondary=3");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("NetworkId");

                    b.HasIndex("Type", "NetworkId")
                        .IsUnique();

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Route", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("DestinationTokenId")
                        .HasColumnType("integer");

                    b.Property<decimal>("MaxAmountInSource")
                        .HasColumnType("numeric");

                    b.Property<int>("SourceTokenId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasComment("Active=0,Inactive=1,Archived=2");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("DestinationTokenId");

                    b.HasIndex("SourceTokenId", "DestinationTokenId")
                        .IsUnique();

                    b.ToTable("Routes");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.ServiceFee", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("DestinationAsset")
                        .HasColumnType("text");

                    b.Property<string>("DestinationNetwork")
                        .HasColumnType("text");

                    b.Property<decimal>("FeeInUsd")
                        .HasColumnType("numeric");

                    b.Property<decimal>("FeePercentage")
                        .HasColumnType("numeric");

                    b.Property<string>("SourceAsset")
                        .HasColumnType("text");

                    b.Property<string>("SourceNetwork")
                        .HasColumnType("text");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.ToTable("ServiceFees");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Swap", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("DestinationAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("DestinationAmount")
                        .HasColumnType("numeric");

                    b.Property<int>("DestinationTokenId")
                        .HasColumnType("integer");

                    b.Property<decimal>("FeeAmount")
                        .HasColumnType("numeric");

                    b.Property<string>("Hashlock")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SourceAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("SourceAmount")
                        .HasColumnType("numeric");

                    b.Property<int>("SourceTokenId")
                        .HasColumnType("integer");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("CreatedDate");

                    b.HasIndex("DestinationAddress");

                    b.HasIndex("DestinationTokenId");

                    b.HasIndex("SourceAddress");

                    b.HasIndex("SourceTokenId");

                    b.ToTable("Swaps");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Token", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Asset")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("Decimals")
                        .HasColumnType("integer");

                    b.Property<bool>("IsNative")
                        .HasColumnType("boolean");

                    b.Property<string>("Logo")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("NetworkId")
                        .HasColumnType("integer");

                    b.Property<int>("Precision")
                        .HasColumnType("integer");

                    b.Property<string>("TokenContract")
                        .HasColumnType("text");

                    b.Property<int>("TokenPriceId")
                        .HasColumnType("integer");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("TokenPriceId");

                    b.HasIndex("NetworkId", "Asset")
                        .IsUnique();

                    b.ToTable("Tokens");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.TokenPrice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("PriceInUsd")
                        .HasColumnType("numeric");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.ToTable("TokenPrices");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Transaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric");

                    b.Property<string>("Asset")
                        .HasColumnType("text");

                    b.Property<int>("Confirmations")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<decimal?>("FeeAmount")
                        .HasColumnType("numeric");

                    b.Property<string>("FeeAsset")
                        .HasColumnType("text");

                    b.Property<decimal?>("FeeUsdPrice")
                        .HasColumnType("numeric");

                    b.Property<string>("NetworkName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasComment("Completed=0,Initiated=1,Failed=2");

                    b.Property<string>("SwapId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TransactionId")
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6");

                    b.Property<decimal>("UsdPrice")
                        .HasColumnType("numeric");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("Status");

                    b.HasIndex("SwapId");

                    b.HasIndex("TransactionId");

                    b.HasIndex("Type");

                    b.HasIndex("TransactionId", "NetworkName")
                        .IsUnique();

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Contract", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Network", "Network")
                        .WithMany("Contracts")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Expense", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "FeeToken")
                        .WithMany()
                        .HasForeignKey("FeeTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "Token")
                        .WithMany()
                        .HasForeignKey("TokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FeeToken");

                    b.Navigation("Token");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.ManagedAccount", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Network", "Network")
                        .WithMany("ManagedAccounts")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Node", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Network", "Network")
                        .WithMany("Nodes")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Route", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "DestinationToken")
                        .WithMany()
                        .HasForeignKey("DestinationTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "SourceToken")
                        .WithMany()
                        .HasForeignKey("SourceTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DestinationToken");

                    b.Navigation("SourceToken");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Swap", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "DestinationToken")
                        .WithMany()
                        .HasForeignKey("DestinationTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Token", "SourceToken")
                        .WithMany()
                        .HasForeignKey("SourceTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DestinationToken");

                    b.Navigation("SourceToken");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Token", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Network", "Network")
                        .WithMany("Tokens")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.TokenPrice", "TokenPrice")
                        .WithMany()
                        .HasForeignKey("TokenPriceId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Network");

                    b.Navigation("TokenPrice");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Transaction", b =>
                {
                    b.HasOne("Train.Solver.Infrastructure.Abstractions.Entities.Swap", "Swap")
                        .WithMany("Transactions")
                        .HasForeignKey("SwapId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Swap");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Network", b =>
                {
                    b.Navigation("Contracts");

                    b.Navigation("ManagedAccounts");

                    b.Navigation("Nodes");

                    b.Navigation("Tokens");
                });

            modelBuilder.Entity("Train.Solver.Infrastructure.Abstractions.Entities.Swap", b =>
                {
                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
