using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Infrastructure.Data;

public sealed class ApplicationDbContextFactoryService(IDbContextFactory<ApplicationDbContext> factory) : IApplicationDbContextFactory
{
    public IApplicationDbContext CreateDbContext() => factory.CreateDbContext();
}
