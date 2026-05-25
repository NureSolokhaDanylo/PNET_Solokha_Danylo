using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Sales.Queries.GetSales;

public class SaleDto
{
    public int SaleId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal SoldPrice { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}

public record SalesQueryResult(List<SaleDto> Items, int TotalCount);

public record GetSalesQuery(
    string? SearchTerm = null,
    int? MedicineId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 10
) : IRequest<SalesQueryResult>;

public class GetSalesQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSalesQueryHandler> logger
) : IRequestHandler<GetSalesQuery, SalesQueryResult>
{
    public async Task<SalesQueryResult> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSalesQuery: SearchTerm={SearchTerm}, MedicineId={MedicineId}, StartDate={StartDate}, EndDate={EndDate}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.MedicineId, request.StartDate, request.EndDate, request.Skip, request.Take);

        try
        {
            using var context = contextFactory.CreateDbContext();

            var baseQuery = from s in context.Sales
                            join m in context.Medicines on s.MedicineId equals m.MedicineId into medGroup
                            from med in medGroup.DefaultIfEmpty()
                            select new SaleDto
                            {
                                SaleId = s.SaleId,
                                MedicineId = s.MedicineId,
                                MedicineName = med != null ? med.Name : "Unknown",
                                Quantity = s.Quantity,
                                BasePrice = med != null ? med.BasePrice : 0,
                                SoldPrice = s.SoldPrice,
                                SaleDate = s.SaleDate,
                                Discount = s.Discount,
                                TotalPrice = (s.SoldPrice * s.Quantity) - s.Discount
                            };

            // Applying filters
            if (request.MedicineId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.MedicineId == request.MedicineId.Value);
            }

            if (request.StartDate.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.SaleDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                // Set to end of the day to include all sales on that day
                var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(x => x.SaleDate <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                baseQuery = baseQuery.Where(x => x.MedicineName.ToLower().Contains(term));
            }

            int totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(x => x.SaleDate)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Successfully fetched {Count} (out of {Total}) Sales.", items.Count, totalCount);
            return new SalesQueryResult(items, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while handling GetSalesQuery.");
            throw;
        }
    }
}
