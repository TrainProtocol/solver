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
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => new { x.Address, x.NetworkType });

        modelBuilder.Entity<Wallet>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<ServiceFee>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Route>()
            .Property(b => b.Status)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => new { x.TransactionHash, x.NetworkName })
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
