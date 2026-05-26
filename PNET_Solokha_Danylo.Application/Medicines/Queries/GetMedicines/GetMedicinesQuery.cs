using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Medicines.Queries.GetMedicines;

public record MedicineQueryResult(List<Medicine> Items, int TotalCount);

public record GetMedicinesQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
    int? SupplierId = null,
    bool? IsActive = null,
    int? Skip = null,
    int? Take = null
) : IRequest<MedicineQueryResult>;

public class GetMedicinesQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetMedicinesQueryHandler> logger
) : IRequestHandler<GetMedicinesQuery, MedicineQueryResult>
{
    public async Task<MedicineQueryResult> Handle(GetMedicinesQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling GetMedicinesQuery: SearchTerm={SearchTerm}, CategoryId={CategoryId}, SupplierId={SupplierId}, IsActive={IsActive}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.CategoryId, request.SupplierId, request.IsActive, request.Skip, request.Take);

        using var context = contextFactory.CreateDbContext();
        var baseQuery = context.Medicines
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            baseQuery = baseQuery.Where(m => 
                m.Name.ToLower().Contains(term));
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            baseQuery = baseQuery.Where(m => m.CategoryId == request.CategoryId.Value);
        }

        if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
        {
            baseQuery = baseQuery.Where(m => m.SupplierId == request.SupplierId.Value);
        }

        if (request.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(m => m.IsActive == request.IsActive.Value);
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        var orderedQuery = baseQuery.OrderBy(m => m.Name);

        List<Medicine> items;
        if (request.Skip.HasValue && request.Take.HasValue)
        {
            items = await orderedQuery
                .Skip(request.Skip.Value)
                .Take(request.Take.Value)
                .ToListAsync(cancellationToken);
        }
        else
        {
            items = await orderedQuery.ToListAsync(cancellationToken);
        }

        return new MedicineQueryResult(items, totalCount);
    }
}
