using FluentValidation;
using MediatR;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Medicines.Commands.CreateMedicine;

public record CreateMedicineCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }
    public decimal BasePrice { get; set; }
}

public class CreateMedicineCommandValidator : AbstractValidator<CreateMedicineCommand>
{
    public CreateMedicineCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("Category is required.");

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("Supplier is required.");

        RuleFor(v => v.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be non-negative.");
    }
}

public class CreateMedicineCommandHandler(IApplicationDbContextFactory contextFactory, ILogger<CreateMedicineCommandHandler> logger) : IRequestHandler<CreateMedicineCommand, int>
{
    public async Task<int> Handle(CreateMedicineCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling CreateMedicineCommand for {Name}", request.Name);
        
        try
        {
            using var context = contextFactory.CreateDbContext();
            var entity = new Medicine(request.Name, request.CategoryId, request.SupplierId, request.BasePrice);

            context.Medicines.Add(entity);

            await context.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully created medicine {Name} with ID {Id}", request.Name, entity.MedicineId);

            return entity.MedicineId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create medicine {Name}", request.Name);
            throw;
        }
    }
}
