using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Inventories.Queries.GetInventoryDetails;

public class InventoryDetailsDto
{
    public int InventoryId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Location { get; set; }
    public decimal MedicineBasePrice { get; set; }
    
    // Statistics
    public decimal Valuation => Quantity * MedicineBasePrice;
    public double DaysRemaining => (ExpiryDate - DateTime.Today).TotalDays;
}

public record GetInventoryDetailsQuery(int InventoryId) : IRequest<InventoryDetailsDto?>;

public class GetInventoryDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetInventoryDetailsQueryHandler> logger
) : IRequestHandler<GetInventoryDetailsQuery, InventoryDetailsDto?>
{
    public async Task<InventoryDetailsDto?> Handle(GetInventoryDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetInventoryDetailsQuery for InventoryId={InventoryId}", request.InventoryId);
        
        using var context = contextFactory.CreateDbContext();
        
        var inventory = await context.Inventories
            .Include(i => i.Medicine)
            .FirstOrDefaultAsync(i => i.InventoryId == request.InventoryId, cancellationToken);
            
        if (inventory == null)
        {
            logger.LogWarning("Inventory with ID {InventoryId} not found.", request.InventoryId);
            return null;
        }
        
        return new InventoryDetailsDto
        {
            InventoryId = inventory.InventoryId,
            MedicineId = inventory.MedicineId,
            MedicineName = inventory.Medicine?.Name ?? "Unknown Medicine",
            BatchNumber = inventory.BatchNumber,
            Quantity = inventory.Quantity,
            ExpiryDate = inventory.ExpiryDate,
            Location = inventory.Location,
            MedicineBasePrice = inventory.Medicine?.BasePrice ?? 0
        };
    }
}
