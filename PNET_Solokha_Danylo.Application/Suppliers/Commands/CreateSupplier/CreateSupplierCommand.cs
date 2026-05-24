using FluentValidation;
using MediatR;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand : IRequest<Unit>
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country must not exceed 50 characters.");
    }
}

public class CreateSupplierCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateSupplierCommand, Unit>
{
    public async Task<Unit> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        // Using the stored procedure as requested
        await context.InsertSupplierAsync(request.Name, request.Country, request.Notes);
        
        return Unit.Value;
    }
}
