using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Medicines.Commands.UpdateMedicine;

public record UpdateMedicineCommand : IRequest
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateMedicineCommandValidator : AbstractValidator<UpdateMedicineCommand>
{
    public UpdateMedicineCommandValidator()
    {
        RuleFor(v => v.MedicineId)
            .GreaterThan(0).WithMessage("MedicineId must be a positive number.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("Category is required.");

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("Supplier is required.");

        RuleFor(v => v.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base Price must be greater than or equal to 0.");
    }
}

public class UpdateMedicineCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<UpdateMedicineCommandHandler> logger
) : IRequestHandler<UpdateMedicineCommand>
{
    public async Task Handle(UpdateMedicineCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling UpdateMedicineCommand for MedicineId={MedicineId}", request.MedicineId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Medicines
            .FirstOrDefaultAsync(m => m.MedicineId == request.MedicineId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Medicine with ID {MedicineId} not found for update.", request.MedicineId);
            throw new KeyNotFoundException($"Medicine with ID {request.MedicineId} was not found.");
        }

        // Verify category exists
        var categoryExists = await context.Categories
            .AnyAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            logger.LogWarning("Category with ID {CategoryId} not found.", request.CategoryId);
            throw new ArgumentException($"Selected category does not exist.");
        }

        // Verify supplier exists
        var supplierExists = await context.Suppliers
            .AnyAsync(s => s.SupplierId == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            logger.LogWarning("Supplier with ID {SupplierId} not found.", request.SupplierId);
            throw new ArgumentException($"Selected supplier does not exist.");
        }

        // Update basic info
        entity.Update(request.Name, request.CategoryId, request.SupplierId, request.BasePrice);

        // Update IsActive status
        if (request.IsActive)
        {
            entity.Activate();
        }
        else
        {
            entity.Deactivate();
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully updated medicine {MedicineId} — new name: {Name}", request.MedicineId, request.Name);
    }
}
