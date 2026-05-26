using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand : IRequest
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("SupplierId must be a positive number.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country must not exceed 50 characters.");
    }
}

public class UpdateSupplierCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<UpdateSupplierCommandHandler> logger
) : IRequestHandler<UpdateSupplierCommand>
{
    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling UpdateSupplierCommand for SupplierId={SupplierId}", request.SupplierId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierId == request.SupplierId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Supplier with ID {SupplierId} not found for update.", request.SupplierId);
            throw new KeyNotFoundException($"Supplier with ID {request.SupplierId} was not found.");
        }

        // LastAuditDate is NOT modified — it's managed automatically by triggers / MarkAuditCompleted()
        entity.Update(request.Name, request.Country, request.Notes);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update supplier {SupplierId}", request.SupplierId);
            throw;
        }

        logger.LogInformation("Successfully updated supplier {SupplierId} — new name: {Name}", request.SupplierId, request.Name);
    }
}
