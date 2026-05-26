using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Inventories.Commands.DeleteInventory;

public record DeleteInventoryCommand(int InventoryId) : IRequest;

public class DeleteInventoryCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<DeleteInventoryCommandHandler> logger
) : IRequestHandler<DeleteInventoryCommand>
{
    public async Task Handle(DeleteInventoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling DeleteInventoryCommand for InventoryId={InventoryId}", request.InventoryId);

        using var context = contextFactory.CreateDbContext();

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.InventoryId == request.InventoryId, cancellationToken);

        if (inventory is null)
        {
            logger.LogWarning("Inventory item with ID {InventoryId} not found for deletion.", request.InventoryId);
            throw new KeyNotFoundException($"Inventory item with ID {request.InventoryId} was not found.");
        }

        var medicine = await context.Medicines
            .FirstOrDefaultAsync(m => m.MedicineId == inventory.MedicineId, cancellationToken);

        if (medicine is null)
        {
            logger.LogWarning("Parent Medicine with ID {MedicineId} not found for inventory item {InventoryId}.", inventory.MedicineId, request.InventoryId);
            throw new KeyNotFoundException($"Associated medicine with ID {inventory.MedicineId} was not found.");
        }

        // Deduct full batch quantity from parent medicine stock
        medicine.SetStock(medicine.TotalStock - inventory.Quantity);

        context.Inventories.Remove(inventory);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete inventory item {InventoryId}.", request.InventoryId);
            throw;
        }

        logger.LogInformation("Successfully deleted inventory batch {InventoryId} — deducted quantity {Qty} from medicine {MedicineName}.",
            request.InventoryId, inventory.Quantity, medicine.Name);
    }
}
