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

namespace PNET_Solokha_Danylo.Application.Medicines.Queries.GetMedicineDetails;

public class MedicineInventoryDto
{
    public int InventoryId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Location { get; set; }
}

public class MedicineSaleDto
{
    public int SaleId { get; set; }
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal SoldPrice { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Discount { get; set; }
    /// <summary>Final paid amount: (SoldPrice × Qty) − Discount.</summary>
    public decimal TotalPrice => (Quantity * SoldPrice) - Discount;
}

public class MedicinePriceChangeDto
{
    public int LogId { get; set; }
    public DateTime LogDate { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserName { get; set; }
}

public class MedicineDetailsDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; }
    
    // Statistics
    public decimal TotalRevenue { get; set; }
    public int TotalUnitsSold { get; set; }
    public int ActiveBatchesCount { get; set; }
    
    // Lists
    public List<MedicineInventoryDto> Inventories { get; set; } = new();
    public List<MedicineSaleDto> Sales { get; set; } = new();
    public List<MedicinePriceChangeDto> PriceChanges { get; set; } = new();
}

public record GetMedicineDetailsQuery(int MedicineId) : IRequest<MedicineDetailsDto?>;

public class GetMedicineDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetMedicineDetailsQueryHandler> logger
) : IRequestHandler<GetMedicineDetailsQuery, MedicineDetailsDto?>
{
    public async Task<MedicineDetailsDto?> Handle(GetMedicineDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling GetMedicineDetailsQuery for MedicineId={MedicineId}", request.MedicineId);
        
        using var context = contextFactory.CreateDbContext();
        
        var medicine = await context.Medicines
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .FirstOrDefaultAsync(m => m.MedicineId == request.MedicineId, cancellationToken);
            
        if (medicine == null)
        {
            logger.LogWarning("Medicine with ID {MedicineId} not found.", request.MedicineId);
            return null;
        }
        
        // Sales statistics
        var sales = await context.Sales
            .Where(s => s.MedicineId == request.MedicineId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);
            
        var saleDtos = sales.Select(s => new MedicineSaleDto
        {
            SaleId = s.SaleId,
            Quantity = s.Quantity,
            BasePrice = medicine.BasePrice,
            SoldPrice = s.SoldPrice,
            SaleDate = s.SaleDate,
            Discount = s.Discount
        }).ToList();
        
        var totalRevenue = saleDtos.Sum(s => s.TotalPrice);
        var totalUnitsSold = saleDtos.Sum(s => s.Quantity);
        
        // Inventory batches
        var inventories = await context.Inventories
            .Where(i => i.MedicineId == request.MedicineId)
            .OrderBy(i => i.ExpiryDate)
            .ToListAsync(cancellationToken);
            
        var inventoryDtos = inventories.Select(i => new MedicineInventoryDto
        {
            InventoryId = i.InventoryId,
            BatchNumber = i.BatchNumber,
            Quantity = i.Quantity,
            ExpiryDate = i.ExpiryDate,
            Location = i.Location
        }).ToList();
        
        var activeBatchesCount = inventoryDtos.Count(i => i.Quantity > 0);
        
        // Price history from audit logs
        var priceAudits = await context.SystemAudit
            .Where(a => a.TableName == "Medicines" && a.RecordId == request.MedicineId && (a.ColumnName == "BasePrice" || a.ActionType == "PRICE_CHANGE"))
            .OrderByDescending(a => a.LogDate)
            .ToListAsync(cancellationToken);
            
        var priceChanges = priceAudits.Select(a => new MedicinePriceChangeDto
        {
            LogId = a.LogId,
            LogDate = a.LogDate,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            UserName = a.UserName
        }).ToList();
        
        return new MedicineDetailsDto
        {
            MedicineId = medicine.MedicineId,
            Name = medicine.Name,
            CategoryId = medicine.CategoryId,
            CategoryName = medicine.Category?.Name ?? "General",
            SupplierId = medicine.SupplierId,
            SupplierName = medicine.Supplier?.Name ?? "Unknown",
            BasePrice = medicine.BasePrice,
            TotalStock = medicine.TotalStock,
            IsActive = medicine.IsActive,
            TotalRevenue = totalRevenue,
            TotalUnitsSold = totalUnitsSold,
            ActiveBatchesCount = activeBatchesCount,
            Inventories = inventoryDtos,
            Sales = saleDtos,
            PriceChanges = priceChanges
        };
    }
}
