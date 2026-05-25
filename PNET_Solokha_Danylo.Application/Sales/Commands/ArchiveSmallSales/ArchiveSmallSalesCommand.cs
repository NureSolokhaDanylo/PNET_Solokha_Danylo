using MediatR;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Sales.Commands.ArchiveSmallSales;

public record ArchiveSmallSalesCommand(int CategoryId, int LimitK) : IRequest;

public class ArchiveSmallSalesCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<ArchiveSmallSalesCommandHandler> logger
) : IRequestHandler<ArchiveSmallSalesCommand>
{
    public async Task Handle(ArchiveSmallSalesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling ArchiveSmallSalesCommand: CategoryId={CategoryId}, LimitK={LimitK}", request.CategoryId, request.LimitK);

        try
        {
            using var context = contextFactory.CreateDbContext();
            await context.ArchiveSmallSalesByCategoryAsync(request.CategoryId, request.LimitK);
            logger.LogInformation("Successfully executed stored procedure to archive small sales for CategoryId={CategoryId} with k={LimitK}.", request.CategoryId, request.LimitK);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute archiving for Category ID {CategoryId} with limit k={LimitK}.", request.CategoryId, request.LimitK);
            throw;
        }
    }
}
