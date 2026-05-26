using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.SalesArchives.Queries.GetSalesArchiveDetails;

public class SalesArchiveDetailsDto
{
    public int ArchiveId { get; set; }
    public int? SaleId { get; set; }
    public int? MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal SoldPrice { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? Reason { get; set; }
    public DateTime ArchivedAt { get; set; }
    
    // Statistics
    public decimal ArchivedRevenue => Quantity * SoldPrice;
    public double ArchivalDelayDays => SaleDate.HasValue ? (ArchivedAt - SaleDate.Value).TotalDays : 0;
}

public record GetSalesArchiveDetailsQuery(int ArchiveId) : IRequest<SalesArchiveDetailsDto?>;

public class GetSalesArchiveDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSalesArchiveDetailsQueryHandler> logger
) : IRequestHandler<GetSalesArchiveDetailsQuery, SalesArchiveDetailsDto?>
{
    public async Task<SalesArchiveDetailsDto?> Handle(GetSalesArchiveDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling GetSalesArchiveDetailsQuery for ArchiveId={ArchiveId}", request.ArchiveId);
        
        using var context = contextFactory.CreateDbContext();
        
        var archive = await context.SalesArchive
            .FirstOrDefaultAsync(sa => sa.ArchiveId == request.ArchiveId, cancellationToken);
            
        if (archive == null)
        {
            logger.LogWarning("Sales archive with ID {ArchiveId} not found.", request.ArchiveId);
            return null;
        }
        
        // Fetch Medicine Name if MedicineId is available
        string medicineName = "Unknown Medicine";
        decimal soldPrice = 0;
        
        if (archive.MedicineId.HasValue)
        {
            var med = await context.Medicines
                .FirstOrDefaultAsync(m => m.MedicineId == archive.MedicineId.Value, cancellationToken);
            if (med != null)
            {
                medicineName = med.Name;
                soldPrice = med.BasePrice; // fallback if sold price not recorded in archive
            }
        }
        
        // Let's check if there is an original sale record to get the sold price, if available
        if (archive.SaleId.HasValue)
        {
            var sale = await context.Sales.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.SaleId == archive.SaleId.Value, cancellationToken);
            if (sale != null)
            {
                soldPrice = sale.SoldPrice;
            }
        }
        
        return new SalesArchiveDetailsDto
        {
            ArchiveId = archive.ArchiveId,
            SaleId = archive.SaleId,
            MedicineId = archive.MedicineId,
            MedicineName = medicineName,
            Quantity = archive.Quantity ?? 0,
            SoldPrice = soldPrice,
            SaleDate = archive.SaleDate,
            Reason = archive.Reason,
            ArchivedAt = archive.ArchivedAt
        };
    }
}
