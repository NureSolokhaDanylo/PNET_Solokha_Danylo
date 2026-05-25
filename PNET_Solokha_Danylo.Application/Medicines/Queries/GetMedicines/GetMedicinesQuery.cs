using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Medicines.Queries.GetMedicines;

public record GetMedicinesQuery : IRequest<List<Medicine>>;

public class GetMedicinesQueryHandler(IApplicationDbContextFactory contextFactory) : IRequestHandler<GetMedicinesQuery, List<Medicine>>
{
    public async Task<List<Medicine>> Handle(GetMedicinesQuery request, CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();
        return await context.Medicines
            .Include(m => m.Category)
            .Include(m => m.Supplier)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }
}
