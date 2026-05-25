using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.SalesArchives.Queries.GetSalesArchives;

public class SalesArchiveDto
{
    public int ArchiveId { get; set; }
    public int? SaleId { get; set; }
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? Quantity { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? Reason { get; set; }
    public DateTime ArchivedAt { get; set; }
}

public record SalesArchiveQueryResult(List<SalesArchiveDto> Items, int TotalCount);

public record GetSalesArchivesQuery(
    string? SearchTerm = null,
    int? MedicineId = null,
    int? SaleId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 10
) : IRequest<SalesArchiveQueryResult>;

public class GetSalesArchivesQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSalesArchivesQueryHandler> logger
) : IRequestHandler<GetSalesArchivesQuery, SalesArchiveQueryResult>
{
    public async Task<SalesArchiveQueryResult> Handle(GetSalesArchivesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSalesArchivesQuery: SearchTerm={SearchTerm}, MedicineId={MedicineId}, SaleId={SaleId}, StartDate={StartDate}, EndDate={EndDate}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.MedicineId, request.SaleId, request.StartDate, request.EndDate, request.Skip, request.Take);

        try
        {
            using var context = contextFactory.CreateDbContext();

            var baseQuery = from sa in context.SalesArchive
                            join m in context.Medicines on sa.MedicineId equals m.MedicineId into medGroup
                            from med in medGroup.DefaultIfEmpty()
                            select new SalesArchiveDto
                            {
                                ArchiveId = sa.ArchiveId,
                                SaleId = sa.SaleId,
                                MedicineId = sa.MedicineId,
                                MedicineName = med != null ? med.Name : "Unknown",
                                Quantity = sa.Quantity,
                                SaleDate = sa.SaleDate,
                                Reason = sa.Reason,
                                ArchivedAt = sa.ArchivedAt
                            };

            // Applying filters
            if (request.MedicineId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.MedicineId == request.MedicineId.Value);
            }

            if (request.SaleId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.SaleId == request.SaleId.Value);
            }

            if (request.StartDate.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.ArchivedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.ArchivedAt <= request.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                baseQuery = baseQuery.Where(x => 
                    (x.Reason != null && x.Reason.ToLower().Contains(term)) ||
                    (x.MedicineName != null && x.MedicineName.ToLower().Contains(term)));
            }

            int totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(x => x.ArchivedAt)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Successfully fetched {Count} (out of {Total}) SalesArchives.", items.Count, totalCount);
            return new SalesArchiveQueryResult(items, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while handling GetSalesArchivesQuery.");
            throw;
        }
    }
}
