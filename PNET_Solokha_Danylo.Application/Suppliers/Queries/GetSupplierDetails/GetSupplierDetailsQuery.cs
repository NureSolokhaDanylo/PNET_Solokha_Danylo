using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Suppliers.Queries.GetSupplierDetails;

public class SupplierMedicineDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierDetailsDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime LastAuditDate { get; set; }
    
    // Statistics
    public int TotalMedicines { get; set; }
    public int TotalStock { get; set; }
    public decimal SupplierStockValue { get; set; }
    
    public string? TopMedicineName { get; set; }
    public decimal TopMedicineRevenue { get; set; }
    
    // Relationships
    public List<SupplierMedicineDto> Medicines { get; set; } = new();
}

public record GetSupplierDetailsQuery(int SupplierId) : IRequest<SupplierDetailsDto?>;

public class GetSupplierDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSupplierDetailsQueryHandler> logger
) : IRequestHandler<GetSupplierDetailsQuery, SupplierDetailsDto?>
{
    public async Task<SupplierDetailsDto?> Handle(GetSupplierDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSupplierDetailsQuery for SupplierId={SupplierId}", request.SupplierId);
        
        using var context = contextFactory.CreateDbContext();
        
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == request.SupplierId, cancellationToken);
            
        if (supplier == null)
        {
            logger.LogWarning("Supplier with ID {SupplierId} not found.", request.SupplierId);
            return null;
        }
        
        var medicines = await context.Medicines
            .Where(m => m.SupplierId == request.SupplierId)
            .ToListAsync(cancellationToken);
            
        var medicineDtos = medicines.Select(m => new SupplierMedicineDto
        {
            MedicineId = m.MedicineId,
            Name = m.Name,
            BasePrice = m.BasePrice,
            TotalStock = m.TotalStock,
            IsActive = m.IsActive
        }).ToList();
        
        var totalMedicines = medicineDtos.Count;
        var totalStock = medicineDtos.Sum(m => m.TotalStock);
        
        // Supplier Stock Value: sum of Quantity * BasePrice for all inventory batches of medicines supplied by this supplier
        var supplierStockValue = await context.Inventories
            .Where(i => i.Medicine.SupplierId == request.SupplierId)
            .SumAsync(i => (decimal?)i.Quantity * i.Medicine.BasePrice, cancellationToken) ?? 0;
            
        // Top Medicine by Revenue
        var topMedicineInfo = await context.Sales
            .Where(s => s.Medicine.SupplierId == request.SupplierId)
            .GroupBy(s => new { s.MedicineId, s.Medicine.Name })
            .Select(g => new
            {
                g.Key.Name,
                Revenue = g.Sum(s => s.Quantity * s.SoldPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefaultAsync(cancellationToken);
            
        return new SupplierDetailsDto
        {
            SupplierId = supplier.SupplierId,
            Name = supplier.Name,
            Country = supplier.Country,
            Notes = supplier.Notes,
            LastAuditDate = supplier.LastAuditDate,
            TotalMedicines = totalMedicines,
            TotalStock = totalStock,
            SupplierStockValue = supplierStockValue,
            TopMedicineName = topMedicineInfo?.Name,
            TopMedicineRevenue = topMedicineInfo?.Revenue ?? 0,
            Medicines = medicineDtos
        };
    }
}
