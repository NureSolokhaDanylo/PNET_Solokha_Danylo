using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Medicines.Queries.GetExpensiveMedicinesCount;

public record GetExpensiveMedicinesCountQuery(string Country) : IRequest<int>;

public class GetExpensiveMedicinesCountQueryHandler(
    IApplicationDbContextFactory contextFactory
) : IRequestHandler<GetExpensiveMedicinesCountQuery, int>
{
    public async Task<int> Handle(GetExpensiveMedicinesCountQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Country))
            return 0;

        using var context = contextFactory.CreateDbContext();
        
        return await context.GetExpensiveMedicinesCountAsync(request.Country, cancellationToken);
    }
}
