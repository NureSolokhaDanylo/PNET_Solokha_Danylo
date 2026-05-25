using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Categories.Queries.GetCategories;

public record CategoryQueryResult(List<Category> Items, int TotalCount);

public record GetCategoriesQuery(
    string? SearchTerm = null,
    int? Skip = null,
    int? Take = null
) : IRequest<CategoryQueryResult>;

public class GetCategoriesQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetCategoriesQueryHandler> logger
) : IRequestHandler<GetCategoriesQuery, CategoryQueryResult>
{
    public async Task<CategoryQueryResult> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetCategoriesQuery: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.Skip, request.Take);

        using var context = contextFactory.CreateDbContext();
        var baseQuery = context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            baseQuery = baseQuery.Where(c => 
                c.Name.ToLower().Contains(term) || 
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        var orderedQuery = baseQuery.OrderBy(c => c.Name);

        List<Category> items;
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

        return new CategoryQueryResult(items, totalCount);
    }
}
