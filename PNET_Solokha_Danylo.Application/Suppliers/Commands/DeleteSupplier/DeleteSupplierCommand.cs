using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Suppliers.Commands.DeleteSupplier;

public record DeleteSupplierCommand(int SupplierId) : IRequest;

public class DeleteSupplierCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<DeleteSupplierCommandHandler> logger
) : IRequestHandler<DeleteSupplierCommand>
{
    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling DeleteSupplierCommand for SupplierId={SupplierId}", request.SupplierId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == request.SupplierId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Supplier with ID {SupplierId} not found for deletion.", request.SupplierId);
            throw new KeyNotFoundException($"Supplier with ID {request.SupplierId} was not found.");
        }

        context.Suppliers.Remove(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully deleted supplier {SupplierId}.", request.SupplierId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Cannot delete supplier {SupplierId} — it has associated medicines.", request.SupplierId);
            throw new InvalidOperationException(
                "Cannot delete this supplier because there are medicines linked to it. " +
                "Please reassign or remove all associated medicines first.", ex);
        }
    }
}
