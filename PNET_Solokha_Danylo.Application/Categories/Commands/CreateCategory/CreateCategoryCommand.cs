using FluentValidation;
using MediatR;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

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

public class CreateCategoryCommandHandler(IApplicationDbContextFactory contextFactory, ILogger<CreateCategoryCommandHandler> logger) : IRequestHandler<CreateCategoryCommand, int>
{
    public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling CreateCategoryCommand for {Name}", request.Name);

        using var context = contextFactory.CreateDbContext();
        var entity = new Category(request.Name, request.Description);

        context.Categories.Add(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create category {Name}", request.Name);
            throw;
        }

        logger.LogInformation("Successfully created category {Name} with ID {Id}", request.Name, entity.CategoryId);

        return entity.CategoryId;
    }
}
