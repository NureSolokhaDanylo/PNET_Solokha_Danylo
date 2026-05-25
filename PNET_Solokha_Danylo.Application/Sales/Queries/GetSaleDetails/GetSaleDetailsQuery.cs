using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Sales.Queries.GetSaleDetails;

public class SaleDetailsDto
{
    public int SaleId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal SoldPrice { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Discount { get; set; }
    public decimal MedicineBasePrice { get; set; }
    
    // Statistics
    public decimal TotalRevenue => Quantity * SoldPrice;
    public decimal OriginalPriceValuation => Quantity * MedicineBasePrice;
    public decimal Savings => Quantity * Discount;
}

public record GetSaleDetailsQuery(int SaleId) : IRequest<SaleDetailsDto?>;

public class GetSaleDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSaleDetailsQueryHandler> logger
) : IRequestHandler<GetSaleDetailsQuery, SaleDetailsDto?>
{
    public async Task<SaleDetailsDto?> Handle(GetSaleDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSaleDetailsQuery for SaleId={SaleId}", request.SaleId);
        
        using var context = contextFactory.CreateDbContext();
        
        var sale = await context.Sales
            .Include(s => s.Medicine)
            .FirstOrDefaultAsync(s => s.SaleId == request.SaleId, cancellationToken);
            
        if (sale == null)
        {
            logger.LogWarning("Sale with ID {SaleId} not found.", request.SaleId);
            return null;
        }
        
        return new SaleDetailsDto
        {
            SaleId = sale.SaleId,
            MedicineId = sale.MedicineId,
            MedicineName = sale.Medicine?.Name ?? "Unknown Medicine",
            Quantity = sale.Quantity,
            SoldPrice = sale.SoldPrice,
            SaleDate = sale.SaleDate,
            Discount = sale.Discount,
            MedicineBasePrice = sale.Medicine?.BasePrice ?? 0
        };
    }
}
