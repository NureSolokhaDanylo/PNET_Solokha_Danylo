using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using PNET_Solokha_Danylo.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.Sales.Commands.CreateSale;

public record CreateSaleCommand : IRequest<int>
{
    public int MedicineId { get; set; }
    public int Quantity { get; set; }
    public decimal SoldPrice { get; set; }
    public decimal Discount { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
}

public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(v => v.MedicineId)
            .GreaterThan(0).WithMessage("Medicine selection is required.");

        RuleFor(v => v.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

        RuleFor(v => v.SoldPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Sold price must be non-negative.");

        RuleFor(v => v.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount must be non-negative.")
            .Must((cmd, discount) => discount <= (cmd.SoldPrice * cmd.Quantity))
            .WithMessage("Discount cannot exceed the total price before discount.");

        RuleFor(v => v.SaleDate)
            .NotEmpty().WithMessage("Sale date is required.");
    }
}

public class CreateSaleCommandHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<CreateSaleCommandHandler> logger
) : IRequestHandler<CreateSaleCommand, int>
{
    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling CreateSaleCommand: MedicineId={MedicineId}, Quantity={Quantity}, SoldPrice={SoldPrice}, Discount={Discount}",
            request.MedicineId, request.Quantity, request.SoldPrice, request.Discount);

        try
        {
            using var context = contextFactory.CreateDbContext();

            var medicine = await context.Medicines.FindAsync(new object[] { request.MedicineId }, cancellationToken);
            if (medicine == null)
            {
                logger.LogWarning("Medicine with ID {MedicineId} not found.", request.MedicineId);
                throw new ArgumentException("Selected medicine does not exist.");
            }

            if (medicine.TotalStock < request.Quantity)
            {
                logger.LogWarning("Not enough stock for medicine {MedicineName}. Available: {Available}, Requested: {Requested}",
                    medicine.Name, medicine.TotalStock, request.Quantity);
                throw new InvalidOperationException($"Not enough stock for medicine '{medicine.Name}'. Available stock: {medicine.TotalStock}.");
            }

            // FIFO: Fetch active inventory batches for this medicine ordered by ExpiryDate
            var batches = await context.Inventories
                .Where(i => i.MedicineId == request.MedicineId && i.Quantity > 0)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync(cancellationToken);

            int remainingToDeduct = request.Quantity;
            foreach (var batch in batches)
            {
                if (remainingToDeduct <= 0)
                    break;

                if (batch.Quantity >= remainingToDeduct)
                {
                    batch.Quantity -= remainingToDeduct;
                    remainingToDeduct = 0;
                }
                else
                {
                    remainingToDeduct -= batch.Quantity;
                    batch.Quantity = 0;
                }
            }

            if (remainingToDeduct > 0)
            {
                logger.LogWarning("Discrepancy detected between Medicine.TotalStock ({TotalStock}) and available quantities in Inventory batches. Shortage: {Shortage}",
                    medicine.TotalStock, remainingToDeduct);
                throw new InvalidOperationException("Not enough items available in active inventory batches to fulfill the sale.");
            }

            // Create sale record
            var entity = new Sale
            {
                MedicineId = request.MedicineId,
                Quantity = request.Quantity,
                SoldPrice = request.SoldPrice,
                Discount = request.Discount,
                SaleDate = request.SaleDate
            };

            // Deduct stock from medicine
            medicine.SetStock(medicine.TotalStock - request.Quantity);

            context.Sales.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully registered sale with ID {SaleId} for medicine {MedicineName}.",
                entity.SaleId, medicine.Name);

            return entity.SaleId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register sale for Medicine ID {MedicineId}.", request.MedicineId);
            throw;
        }
    }
}
