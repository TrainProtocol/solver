using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Data.Npgsql.Extensions;

namespace Train.Solver.Data.Npgsql;

public class SolverDbContext(DbContextOptions<SolverDbContext> options) : DbContext(options)
{
    public DbSet<Swap> Swaps { get; set; }

    public DbSet<SwapMetric> SwapMetrics { get; set; }

    public DbSet<Network> Networks { get; set; }

    public DbSet<Token> Tokens { get; set; }

    public DbSet<TokenPrice> TokenPrices { get; set; }

    public DbSet<RateProvider> RateProviders { get; set; }

    public DbSet<Wallet> Wallets { get; set; }

    public DbSet<SignerAgent> SignerAgents { get; set; }

    public DbSet<TrustedWallet> TrustedWallets { get; set; }

    public DbSet<Node> Nodes { get; set; }

    public DbSet<Transaction> Transactions { get; set; }

    public DbSet<Route> Routes { get; set; }

    public DbSet<ServiceFee> ServiceFees { get; set; }

    public DbSet<Expense> Expenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Swap).Assembly);
        SetupCustomRelations(modelBuilder);

        if (Database.IsNpgsql())
        {
            SetupGuidPrimaryKeyAndConcurrencyToken(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceFee>().HasData(
            new ServiceFee { Id = 100000, Name = "Free", FeeInUsd = 0, FeePercentage = 0 },
            new ServiceFee { Id = 100001, Name = "Default", FeeInUsd = 0, FeePercentage = 0 }
        );

        modelBuilder.Entity<RateProvider>().HasData(
            new RateProvider { Id = 100000, Name = "SameAsset" },
            new RateProvider { Id = 100001, Name = "Binance" }
        );

        var seedDate = new DateTime(2025, 8, 12, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<TokenPrice>().HasData(
            new TokenPrice { Id = 10002,  ExternalId = "bitcoin", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "BTC" },
            new TokenPrice { Id = 10008,  ExternalId = "fuel-network", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "FUEL" },
            new TokenPrice { Id = 10014,  ExternalId = "avalanche-2", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "AVAX" },
            new TokenPrice { Id = 10018,  ExternalId = "optimism", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "OP" },
            new TokenPrice { Id = 10026,  ExternalId = "ethereum", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "ETH" },
            new TokenPrice { Id = 10035,  ExternalId = "solana", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "SOL" },
            new TokenPrice { Id = 10043,  ExternalId = "dai", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "DAI" },
            new TokenPrice { Id = 10046,  ExternalId = "usd-coin", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "USDC" },
            new TokenPrice { Id = 10050,  ExternalId = "immutable-x", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "IMX" },
            new TokenPrice { Id = 10054,  ExternalId = "binancecoin", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "BNB" },
            new TokenPrice { Id = 10055,  ExternalId = "tether", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "USDT" },
            new TokenPrice { Id = 10056,  ExternalId = "matic-network", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "MATIC" },
            new TokenPrice { Id = 10063,  ExternalId = "polygon-ecosystem-token", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "POL" },
            new TokenPrice { Id = 10069,  ExternalId = "ronin", LastUpdated = seedDate, CreatedDate = seedDate, Symbol = "RON" }
        );
    }

    private static void SetupCustomRelations(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Swap>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Swap)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Network>(entity =>
        {
            entity.HasOne(n => n.NativeToken)
                .WithMany()
                .HasForeignKey(n => n.NativeTokenId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Network>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Network>()
            .HasIndex(x => new { x.ChainId, x.Type })
            .IsUnique();

        modelBuilder.Entity<Network>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.Address, x.NetworkType });

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.Name, x.NetworkType }).IsUnique();

        modelBuilder.Entity<TrustedWallet>()
          .HasIndex(x => new { x.Address, x.NetworkType });

        modelBuilder.Entity<TrustedWallet>()
            .HasIndex(x => new { x.Name, x.NetworkType }).IsUnique();

        modelBuilder.Entity<ServiceFee>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<SignerAgent>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Node>()
           .HasIndex(x => new { x.ProviderName, x.NetworkId }).IsUnique();

        modelBuilder.Entity<Route>()
            .Property(b => b.Status)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => new { x.TransactionHash, x.NetworkId })
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .Property(b => b.Status)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.TransactionHash);

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.Type);

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.Status)
            .IsUnique(unique: false);

        modelBuilder.Entity<Token>()
           .HasIndex(x => new { x.NetworkId, x.Asset })
           .IsUnique();

        modelBuilder.Entity<Token>()
           .HasIndex(x => x.Asset);

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.SourceAddress);

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.DestinationAddress);

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.CreatedDate);

        modelBuilder.Entity<Swap>()
          .HasIndex(x => x.CommitId).IsUnique();

        modelBuilder.Entity<SwapMetric>()
            .HasOne(m => m.Swap)
            .WithOne(s => s.Metric)
            .HasForeignKey<SwapMetric>(m => m.SwapId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SwapMetric>()
            .HasIndex(m => m.SourceNetwork);

        modelBuilder.Entity<SwapMetric>()
            .HasIndex(m => m.DestinationNetwork);

        modelBuilder.Entity<SwapMetric>()
            .HasIndex(m => m.CreatedDate);

        modelBuilder.Entity<RateProvider>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<TokenPrice>()
            .HasIndex(x => x.Symbol)
            .IsUnique();

        modelBuilder.Entity<Token>()
           .HasOne(t => t.TokenPrice)
           .WithMany()
           .HasForeignKey(t => t.TokenPriceId)
           .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Expense>()
            .HasIndex(x => new
            {
                x.TokenId,
                x.FeeTokenId,
                x.TransactionType
            })
            .IsUnique();

        modelBuilder.Entity<Expense>()
            .Property(b => b.TransactionType)
            .HasEnumComment();

        modelBuilder.Entity<Route>()
            .HasIndex(x => new { x.SourceTokenId, x.DestinationTokenId })
            .IsUnique();
    }

    private static void SetupGuidPrimaryKeyAndConcurrencyToken(ModelBuilder modelBuilder)
    {
        var entitiesAssambly = typeof(EntityBase).Assembly;

        var entities = entitiesAssambly.GetTypes()
            .Where(x =>
                x.BaseType == typeof(EntityBase)
                && !x.IsAbstract);

        foreach (var entity in entities)
        {
            modelBuilder
                .Entity(entity)
                .Property(nameof(EntityBase.CreatedDate))
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            modelBuilder
                .Entity(entity)
                .Property(nameof(EntityBase.Version))
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("xid")
                .HasColumnName("xmin");
        }
    }
}
