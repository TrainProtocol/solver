﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Train.Solver.Data;

#nullable disable

namespace Train.Solver.Data.Migrations.Migrations
{
    [DbContext(typeof(SolverDbContext))]
    [Migration("20250214112921_RemoveExpenceFromRoute")]
    partial class RemoveExpenceFromRoute
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ContractNetwork", b =>
                {
                    b.Property<int>("DeployedContractsId")
                        .HasColumnType("integer");

                    b.Property<int>("NetworksId")
                        .HasColumnType("integer");

                    b.HasKey("DeployedContractsId", "NetworksId");

                    b.HasIndex("NetworksId");

                    b.ToTable("ContractNetwork");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.App", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiKey")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SandboxApiKey")
                        .HasColumnType("text");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("ApiKey")
                        .IsUnique();

                    b.HasIndex("SandboxApiKey")
                        .IsUnique();

                    b.ToTable("Apps");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Contract", b =>
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

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("HTLCNativeContractAddress=0,HTLCTokenContractAddress=1,GasPriceOracleContract=2,ZKSPaymasterContract=3,EvmMultiCallContract=4,EvmOracleContract=5,WatchdogContractAddress=6");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.ToTable("Contracts");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Deployment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("AppId")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("NetworkName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("AppId");

                    b.ToTable("Deployments");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Expense", b =>
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
                        .HasComment("Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7");

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

            modelBuilder.Entity("Train.Solver.Data.Entities.ManagedAccount", b =>
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

            modelBuilder.Entity("Train.Solver.Data.Entities.Network", b =>
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

                    b.Property<string>("FixedGasPriceInGwei")
                        .HasColumnType("text");

                    b.Property<int?>("GasLimitPercentageIncrease")
                        .HasColumnType("integer");

                    b.Property<int>("Group")
                        .HasColumnType("integer")
                        .HasComment("EVM_LEGACY=0,EVM_EIP1559=1,EVM_ARBITRUM_LEGACY=2,EVM_ARBITRUM_EIP1559=3,EVM_OPTIMISM_EIP1559=4,EVM_OPTIMISM_LEGACY=5,EVM_POLYGON_LEGACY=6,EVM_POLYGON_EIP1559=7,FUEL=8,IMMUTABLEX=9,LOOPRING=10,OSMOSIS=11,SOLANA=12,STARKNET=13,STARKNET_PARADEX=14,TON=15,TRON=16,ZKSYNC=17,BRINE=18,RHINOFI=19,APTOS=20,ZKSPACE=21,ZKSYNC_ERA_PAYMASTER=22");

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

            modelBuilder.Entity("Train.Solver.Data.Entities.Node", b =>
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
                        .HasColumnType("integer");

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

            modelBuilder.Entity("Train.Solver.Data.Entities.ReservedNonce", b =>
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

                    b.Property<string>("Nonce")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ReferenceId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("NetworkId", "ReferenceId")
                        .IsUnique();

                    b.ToTable("ReservedNonces");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Route", b =>
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
                        .HasComment("Active=0,Archived=1,DelayedWithdrawal=2,DelayedDeposit=3,DailyLimitReached=4,UnderMaintenance=5");

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

            modelBuilder.Entity("Train.Solver.Data.Entities.ServiceFee", b =>
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

            modelBuilder.Entity("Train.Solver.Data.Entities.Swap", b =>
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

                    b.HasIndex("DestinationTokenId");

                    b.HasIndex("SourceTokenId");

                    b.ToTable("Swaps");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Token", b =>
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

            modelBuilder.Entity("Train.Solver.Data.Entities.TokenPrice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiSymbol")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<DateTime>("LastUpdated")
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

            modelBuilder.Entity("Train.Solver.Data.Entities.Transaction", b =>
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
                        .HasComment("Completed=0,Initiated=1");

                    b.Property<string>("SwapId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TransactionId")
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasComment("Transfer=0,Approve=1,HTLCCommit=2,HTLCLock=3,HTLCRedeem=4,HTLCRefund=5,HTLCAddLockSig=6,OptimismDeposit=7");

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

            modelBuilder.Entity("ContractNetwork", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Contract", null)
                        .WithMany()
                        .HasForeignKey("DeployedContractsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Data.Entities.Network", null)
                        .WithMany()
                        .HasForeignKey("NetworksId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Deployment", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.App", "App")
                        .WithMany("Deployments")
                        .HasForeignKey("AppId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("App");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Expense", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Token", "FeeToken")
                        .WithMany()
                        .HasForeignKey("FeeTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Data.Entities.Token", "Token")
                        .WithMany()
                        .HasForeignKey("TokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FeeToken");

                    b.Navigation("Token");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.ManagedAccount", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Network", "Network")
                        .WithMany("ManagedAccounts")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Node", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Network", "Network")
                        .WithMany("Nodes")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.ReservedNonce", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Network", "Network")
                        .WithMany()
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Route", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Token", "DestinationToken")
                        .WithMany()
                        .HasForeignKey("DestinationTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Data.Entities.Token", "SourceToken")
                        .WithMany()
                        .HasForeignKey("SourceTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DestinationToken");

                    b.Navigation("SourceToken");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Swap", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Token", "DestinationToken")
                        .WithMany()
                        .HasForeignKey("DestinationTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Data.Entities.Token", "SourceToken")
                        .WithMany()
                        .HasForeignKey("SourceTokenId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DestinationToken");

                    b.Navigation("SourceToken");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Token", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Network", "Network")
                        .WithMany("Tokens")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Train.Solver.Data.Entities.TokenPrice", "TokenPrice")
                        .WithMany()
                        .HasForeignKey("TokenPriceId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Network");

                    b.Navigation("TokenPrice");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Transaction", b =>
                {
                    b.HasOne("Train.Solver.Data.Entities.Swap", "Swap")
                        .WithMany("Transactions")
                        .HasForeignKey("SwapId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Swap");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.App", b =>
                {
                    b.Navigation("Deployments");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Network", b =>
                {
                    b.Navigation("ManagedAccounts");

                    b.Navigation("Nodes");

                    b.Navigation("Tokens");
                });

            modelBuilder.Entity("Train.Solver.Data.Entities.Swap", b =>
                {
                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
