using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand : IRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be a positive number.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");
    }
}

public class UpdateCategoryCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<UpdateCategoryCommandHandler> logger
) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling UpdateCategoryCommand for CategoryId={CategoryId}", request.CategoryId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Category with ID {CategoryId} not found for update.", request.CategoryId);
            throw new KeyNotFoundException($"Category with ID {request.CategoryId} was not found.");
        }

        entity.Update(request.Name, request.Description);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update category {CategoryId}", request.CategoryId);
            throw;
        }

        logger.LogInformation("Successfully updated category {CategoryId} — new name: {Name}", request.CategoryId, request.Name);
    }
}
