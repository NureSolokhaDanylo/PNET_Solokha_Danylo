using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Medicines.Commands.DeleteMedicine;

public record DeleteMedicineCommand(int MedicineId) : IRequest;

public class DeleteMedicineCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<DeleteMedicineCommandHandler> logger
) : IRequestHandler<DeleteMedicineCommand>
{
    public async Task Handle(DeleteMedicineCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling DeleteMedicineCommand for MedicineId={MedicineId}", request.MedicineId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Medicines
            .FirstOrDefaultAsync(m => m.MedicineId == request.MedicineId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Medicine with ID {MedicineId} not found for deletion.", request.MedicineId);
            throw new KeyNotFoundException($"Medicine with ID {request.MedicineId} was not found.");
        }

        context.Medicines.Remove(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully deleted medicine {MedicineId}.", request.MedicineId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Cannot delete medicine {MedicineId} — it has associated sales.", request.MedicineId);
            throw new InvalidOperationException(
                "Cannot delete this medicine because there are sales records linked to it. " +
                "Please deactivate it (IsActive = false) using the Edit form to remove it from the active catalog instead.", ex);
        }
    }
}
