using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Inventories.Queries.GetInventories;

public class InventoryDto
{
    public int InventoryId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Location { get; set; }
}

public record InventoryQueryResult(List<InventoryDto> Items, int TotalCount);

public record GetInventoriesQuery(
    string? SearchTerm = null,
    int? MedicineId = null,
    int Skip = 0,
    int Take = 10
) : IRequest<InventoryQueryResult>;

public class GetInventoriesQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetInventoriesQueryHandler> logger
) : IRequestHandler<GetInventoriesQuery, InventoryQueryResult>
{
    public async Task<InventoryQueryResult> Handle(GetInventoriesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetInventoriesQuery: SearchTerm={SearchTerm}, MedicineId={MedicineId}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.MedicineId, request.Skip, request.Take);

        try
        {
            using var context = contextFactory.CreateDbContext();

            var baseQuery = from inv in context.Inventories
                            join med in context.Medicines on inv.MedicineId equals med.MedicineId
                            select new InventoryDto
                            {
                                InventoryId = inv.InventoryId,
                                MedicineId = inv.MedicineId,
                                MedicineName = med.Name,
                                BatchNumber = inv.BatchNumber,
                                Quantity = inv.Quantity,
                                ExpiryDate = inv.ExpiryDate,
                                Location = inv.Location
                            };

            // Applying filters
            if (request.MedicineId.HasValue && request.MedicineId.Value > 0)
            {
                baseQuery = baseQuery.Where(x => x.MedicineId == request.MedicineId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.BatchNumber.ToLower().Contains(term) ||
                    (x.Location != null && x.Location.ToLower().Contains(term)));
            }

            int totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderBy(x => x.ExpiryDate)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Successfully fetched {Count} (out of {Total}) Inventory items.", items.Count, totalCount);
            return new InventoryQueryResult(items, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while handling GetInventoriesQuery.");
            throw;
        }
    }
}
