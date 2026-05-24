using FluentValidation;
using MediatR;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

namespace PNET_Solokha_Danylo.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");
    }
}

public class CreateCategoryCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateCategoryCommand, int>
{
    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new Category(request.Name, request.Description);

        context.Categories.Add(entity);

        await context.SaveChangesAsync(cancellationToken);

        return entity.CategoryId;
    }
}
