using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Categories.Queries.GetCategoryDetails;

public class CategoryMedicineDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryDetailsDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Statistics
    public int TotalMedicines { get; set; }
    public int TotalStock { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalInventoryValue { get; set; }
    
    // Relationships
    public List<CategoryMedicineDto> Medicines { get; set; } = new();
}

public record GetCategoryDetailsQuery(int CategoryId) : IRequest<CategoryDetailsDto?>;

public class GetCategoryDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetCategoryDetailsQueryHandler> logger
) : IRequestHandler<GetCategoryDetailsQuery, CategoryDetailsDto?>
{
    public async Task<CategoryDetailsDto?> Handle(GetCategoryDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetCategoryDetailsQuery for CategoryId={CategoryId}", request.CategoryId);
        
        using var context = contextFactory.CreateDbContext();
        
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            
        if (category == null)
        {
            logger.LogWarning("Category with ID {CategoryId} not found.", request.CategoryId);
            return null;
        }
        
        var medicines = await context.Medicines
            .Where(m => m.CategoryId == request.CategoryId)
            .ToListAsync(cancellationToken);
            
        var medicineDtos = medicines.Select(m => new CategoryMedicineDto
        {
            MedicineId = m.MedicineId,
            Name = m.Name,
            BasePrice = m.BasePrice,
            TotalStock = m.TotalStock,
            IsActive = m.IsActive
        }).ToList();
        
        var totalMedicines = medicineDtos.Count;
        var totalStock = medicineDtos.Sum(m => m.TotalStock);
        var averagePrice = totalMedicines > 0 ? medicineDtos.Average(m => m.BasePrice) : 0;
        
        // Total Inventory Value
        var totalInventoryValue = await context.Inventories
            .Where(i => i.Medicine.CategoryId == request.CategoryId)
            .SumAsync(i => (decimal?)i.Quantity * i.Medicine.BasePrice, cancellationToken) ?? 0;
            
        return new CategoryDetailsDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            TotalMedicines = totalMedicines,
            TotalStock = totalStock,
            AveragePrice = averagePrice,
            TotalInventoryValue = totalInventoryValue,
            Medicines = medicineDtos
        };
    }
}
