using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PNET_Solokha_Danylo.Application.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(int CategoryId) : IRequest;

public class DeleteCategoryCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<DeleteCategoryCommandHandler> logger
) : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling DeleteCategoryCommand for CategoryId={CategoryId}", request.CategoryId);

        using var context = contextFactory.CreateDbContext();

        var entity = await context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Category with ID {CategoryId} not found for deletion.", request.CategoryId);
            throw new KeyNotFoundException($"Category with ID {request.CategoryId} was not found.");
        }

        context.Categories.Remove(entity);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully deleted category {CategoryId}.", request.CategoryId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Cannot delete category {CategoryId} — it has associated medicines.", request.CategoryId);
            throw new InvalidOperationException(
                "Cannot delete this category because there are medicines linked to it. " +
                "Please reassign or remove all associated medicines first.", ex);
        }
    }
}
