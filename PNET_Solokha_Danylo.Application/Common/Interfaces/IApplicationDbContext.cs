using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;

namespace PNET_Solokha_Danylo.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Medicine> Medicines { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SalesArchive> SalesArchive { get; }
    DbSet<SystemAudit> SystemAudit { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    // Stored Procedures
    Task InsertSupplierAsync(string name, string country, string? notes = null);
    Task ArchiveSmallSalesByCategoryAsync(int categoryId, int k);
}
