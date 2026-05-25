namespace PNET_Solokha_Danylo.Application.Common.Interfaces;

public interface IApplicationDbContextFactory
{
    IApplicationDbContext CreateDbContext();
}
