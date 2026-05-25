using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<List<Category>>;

public class GetCategoriesQueryHandler(IApplicationDbContextFactory contextFactory) : IRequestHandler<GetCategoriesQuery, List<Category>>
{
    public async Task<List<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
