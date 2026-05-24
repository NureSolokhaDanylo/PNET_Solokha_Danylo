using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Suppliers.Queries.GetSuppliers;

public record GetSuppliersQuery : IRequest<List<Supplier>>;

public class GetSuppliersQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSuppliersQuery, List<Supplier>>
{
    public async Task<List<Supplier>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        return await context.Suppliers
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }
}
