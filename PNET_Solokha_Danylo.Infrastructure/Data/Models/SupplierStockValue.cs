using Microsoft.EntityFrameworkCore;

namespace PNET_Solokha_Danylo.Infrastructure.Data;

public class SupplierStockValue
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public decimal TotalValue { get; set; }
}
