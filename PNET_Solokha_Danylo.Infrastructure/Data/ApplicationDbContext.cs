using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;

namespace PNET_Solokha_Danylo.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalesArchive> SalesArchive => Set<SalesArchive>();
    public DbSet<SystemAudit> SystemAudit => Set<SystemAudit>();

    // Table-Valued Function
    public IQueryable<SupplierStockValue> GetSupplierStockValue()
        => FromExpression(() => GetSupplierStockValue());

    // Scalar Function
    public int CountExpensiveMedicines(string country)
        => throw new NotSupportedException();

    // Stored Procedures
    public async Task InsertSupplierAsync(string name, string country, string? notes = null)
    {
        await Database.ExecuteSqlInterpolatedAsync($"EXEC usp_InsertSupplier @Name={name}, @Country={country}, @Notes={notes}");
    }

    public async Task ArchiveSmallSalesByCategoryAsync(int categoryId, int k)
    {
        await Database.ExecuteSqlInterpolatedAsync($"EXEC usp_ArchiveSmallSalesByCategory @CategoryId={categoryId}, @k={k}");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map Functions
        builder.HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(CountExpensiveMedicines), new[] { typeof(string) })!)
            .HasName("fn_CountExpensiveMedicines");

        builder.Entity<SupplierStockValue>(entity =>
        {
            entity.HasNoKey().ToFunction("fn_GetSupplierStockValue");
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
        });

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastAuditDate).HasDefaultValueSql("GETDATE()");
        });

        builder.Entity<Medicine>(entity =>
        {
            entity.ToTable("Medicines", t => t.HasTrigger("tr_LogPriceChanges"));
            entity.HasKey(e => e.MedicineId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalStock).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Medicines)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.Supplier)
                .WithMany(p => p.Medicines)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventory", t => t.HasTrigger("tr_UpdateAuditOnDelivery"));
            entity.HasKey(e => e.InventoryId);
            entity.Property(e => e.BatchNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(50).HasDefaultValue("Main Shelf");

            entity.HasOne(d => d.Medicine)
                .WithMany(p => p.Inventories)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");
            entity.HasKey(e => e.SaleId);
            entity.Property(e => e.SoldPrice).HasPrecision(18, 2);
            entity.Property(e => e.SaleDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Discount).HasPrecision(5, 2).HasDefaultValue(0);

            entity.HasOne(d => d.Medicine)
                .WithMany(p => p.Sales)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<SalesArchive>(entity =>
        {
            entity.ToTable("SalesArchive");
            entity.HasKey(e => e.ArchiveId);
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.ArchivedAt).HasDefaultValueSql("GETDATE()");
        });

        builder.Entity<SystemAudit>(entity =>
        {
            entity.ToTable("SystemAudit");
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.LogDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Severity).HasMaxLength(20).HasDefaultValue("INFO");
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.ColumnName).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(100).HasDefaultValueSql("CURRENT_USER");
        });
    }
}
