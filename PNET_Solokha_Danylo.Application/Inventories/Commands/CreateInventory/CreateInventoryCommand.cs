using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using PNET_Solokha_Danylo.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Inventories.Commands.CreateInventory;

public record CreateInventoryCommand : IRequest<int>
{
    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);
    public int Quantity { get; set; }
    public string? Location { get; set; } = "Main Shelf";
}

public class CreateInventoryCommandValidator : AbstractValidator<CreateInventoryCommand>
{
    public CreateInventoryCommandValidator()
    {
        RuleFor(v => v.MedicineId)
            .GreaterThan(0).WithMessage("Medicine is required.");

        RuleFor(v => v.BatchNumber)
            .NotEmpty().WithMessage("Batch number is required.")
            .MaximumLength(50).WithMessage("Batch number must not exceed 50 characters.");

        RuleFor(v => v.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

        RuleFor(v => v.ExpiryDate)
            .NotEmpty().WithMessage("Expiry date is required.")
            .Must(d => d > DateTime.Today).WithMessage("Expiry date must be in the future.");

        RuleFor(v => v.Location)
            .MaximumLength(50).WithMessage("Location must not exceed 50 characters.");
    }
}

public class CreateInventoryCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<CreateInventoryCommandHandler> logger
) : IRequestHandler<CreateInventoryCommand, int>
{
    public async Task<int> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling CreateInventoryCommand: MedicineId={MedicineId}, BatchNumber={BatchNumber}, Quantity={Quantity}",
            request.MedicineId, request.BatchNumber, request.Quantity);

        using var context = contextFactory.CreateDbContext();

        var medicine = await context.Medicines.FindAsync(new object[] { request.MedicineId }, cancellationToken);
        if (medicine == null)
        {
            logger.LogWarning("Medicine with ID {MedicineId} not found.", request.MedicineId);
            throw new ArgumentException("Selected medicine does not exist.");
        }

        // Create inventory entry
        var entity = new Inventory
        {
            MedicineId = request.MedicineId,
            BatchNumber = request.BatchNumber,
            ExpiryDate = request.ExpiryDate,
            Quantity = request.Quantity,
            Location = request.Location ?? "Main Shelf"
        };

        // Update medicine's total stock
        medicine.SetStock(medicine.TotalStock + request.Quantity);

        context.Inventories.Add(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create inventory item for Medicine ID {MedicineId}.", request.MedicineId);
            throw;
        }

        logger.LogInformation("Successfully created inventory item with ID {InventoryId} for medicine {MedicineName}.",
            entity.InventoryId, medicine.Name);

        return entity.InventoryId;
    }
}
