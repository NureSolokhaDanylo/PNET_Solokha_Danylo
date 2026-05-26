using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Inventories.Commands.UpdateInventory;

public record UpdateInventoryCommand : IRequest
{
    public int InventoryId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public string? Location { get; set; }
}

public class UpdateInventoryCommandValidator : AbstractValidator<UpdateInventoryCommand>
{
    public UpdateInventoryCommandValidator()
    {
        RuleFor(v => v.InventoryId)
            .GreaterThan(0).WithMessage("Inventory ID must be positive.");

        RuleFor(v => v.BatchNumber)
            .NotEmpty().WithMessage("Batch Number is required.")
            .MaximumLength(50).WithMessage("Batch Number must not exceed 50 characters.");

        RuleFor(v => v.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be greater than or equal to 0.");

        RuleFor(v => v.ExpiryDate)
            .NotEmpty().WithMessage("Expiry Date is required.");

        RuleFor(v => v.Location)
            .MaximumLength(50).WithMessage("Location must not exceed 50 characters.");
    }
}

public class UpdateInventoryCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<UpdateInventoryCommandHandler> logger
) : IRequestHandler<UpdateInventoryCommand>
{
    public async Task Handle(UpdateInventoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling UpdateInventoryCommand: InventoryId={InventoryId}, Qty={Quantity}", request.InventoryId, request.Quantity);

        using var context = contextFactory.CreateDbContext();

        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i => i.InventoryId == request.InventoryId, cancellationToken);

        if (inventory is null)
        {
            logger.LogWarning("Inventory item with ID {InventoryId} not found for update.", request.InventoryId);
            throw new KeyNotFoundException($"Inventory item with ID {request.InventoryId} was not found.");
        }

        var medicine = await context.Medicines
            .FirstOrDefaultAsync(m => m.MedicineId == inventory.MedicineId, cancellationToken);

        if (medicine is null)
        {
            logger.LogWarning("Parent Medicine with ID {MedicineId} not found for inventory item {InventoryId}.", inventory.MedicineId, request.InventoryId);
            throw new KeyNotFoundException($"Associated medicine with ID {inventory.MedicineId} was not found.");
        }

        // Calculate discrepancy in stock and adjust Medicine's TotalStock
        int quantityDiff = request.Quantity - inventory.Quantity;
        medicine.SetStock(medicine.TotalStock + quantityDiff);

        if (request.Quantity == 0)
        {
            context.Inventories.Remove(inventory);
        }
        else
        {
            // Update fields
            inventory.BatchNumber = request.BatchNumber;
            inventory.ExpiryDate = request.ExpiryDate;
            inventory.Quantity = request.Quantity;
            inventory.Location = request.Location ?? "Main Shelf";
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update inventory item {InventoryId}.", request.InventoryId);
            throw;
        }

        logger.LogInformation("Successfully updated inventory item {InventoryId} for medicine {MedicineId} — stock diff: {Diff}",
            request.InventoryId, medicine.MedicineId, quantityDiff);
    }
}
