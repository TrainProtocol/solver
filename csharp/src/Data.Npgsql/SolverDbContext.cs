using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Entities.Base;
using Train.Solver.Repositories.Npgsql.Extensions;

namespace Train.Solver.Repositories.Npgsql;

public class SolverDbContext(DbContextOptions<SolverDbContext> options) : DbContext(options)
{
    public DbSet<Swap> Swaps { get; set; }

    public DbSet<Network> Networks { get; set; }

    public DbSet<Token> Tokens { get; set; }

    public DbSet<TokenPrice> TokenPrices { get; set; }

    public DbSet<ManagedAccount> ManagedAccounts { get; set; }

    public DbSet<Node> Nodes { get; set; }

    public DbSet<Transaction> Transactions { get; set; }

    public DbSet<Route> Routes { get; set; }

    public DbSet<ServiceFee> ServiceFees { get; set; }

    public DbSet<Expense> Expenses { get; set; }

    public DbSet<Contract> Contracts { get; set; }

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

        modelBuilder.Entity<Network>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Network>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Contract>()
        .Property(b => b.Type)
        .HasEnumComment();

        modelBuilder.Entity<Node>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Node>()
            .HasIndex(x => new { x.Type, x.NetworkId })
            .IsUnique();

        modelBuilder.Entity<ManagedAccount>()
          .HasIndex(x => x.Address);

        modelBuilder.Entity<ManagedAccount>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Route>()
            .Property(b => b.Status)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => new { x.TransactionId, x.NetworkName })
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .Property(b => b.Status)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .Property(b => b.Type)
            .HasEnumComment();

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.TransactionId);

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.Type);

        modelBuilder.Entity<Transaction>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<Token>()
           .HasIndex(x => new { x.NetworkId, x.Asset })
           .IsUnique();

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.SourceAddress);

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.DestinationAddress);

        modelBuilder.Entity<Swap>()
            .HasIndex(x => x.CreatedDate);

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
        modelBuilder.HasPostgresExtension("uuid-ossp");

        var entitiesAssambly = typeof(EntityBase<>).Assembly;

        var entities = entitiesAssambly.GetTypes()
            .Where(x => 
                x.BaseType is { IsGenericType: true } 
                && x.BaseType.GetGenericTypeDefinition() == typeof(EntityBase<>)
                && !x.IsAbstract);

        foreach (var entity in entities)
        {
            if (entity.BaseType!.GetGenericArguments()[0] == typeof(Guid))
            {
                modelBuilder
                    .Entity(entity)
                    .Property(nameof(EntityBase<Guid>.Id))
                    .HasDefaultValueSql("uuid_generate_v4()");
            }

            modelBuilder
                .Entity(entity)
                .Property(nameof(EntityBase<int>.CreatedDate))
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            modelBuilder
                .Entity(entity)
                .Property(nameof(EntityBase<int>.Version))
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("xid")
                .HasColumnName("xmin");
        }
    }
}
