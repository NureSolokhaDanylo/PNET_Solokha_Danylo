using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Suppliers.Queries.GetSuppliers;

public record SupplierQueryResult(List<Supplier> Items, int TotalCount);

public record GetSuppliersQuery(
    string? SearchTerm = null,
    string? Country = null,
    int? Skip = null,
    int? Take = null
) : IRequest<SupplierQueryResult>;

public class GetSuppliersQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSuppliersQueryHandler> logger
) : IRequestHandler<GetSuppliersQuery, SupplierQueryResult>
{
    public async Task<SupplierQueryResult> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSuppliersQuery: SearchTerm={SearchTerm}, Country={Country}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.Country, request.Skip, request.Take);

        using var context = contextFactory.CreateDbContext();
        var baseQuery = context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            baseQuery = baseQuery.Where(s => 
                s.Name.ToLower().Contains(term) || 
                (s.Notes != null && s.Notes.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var countryTerm = request.Country.Trim().ToLower();
            baseQuery = baseQuery.Where(s => s.Country.ToLower() == countryTerm);
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        var orderedQuery = baseQuery.OrderBy(s => s.Name);

        List<Supplier> items;
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

        return new SupplierQueryResult(items, totalCount);
    }
}
