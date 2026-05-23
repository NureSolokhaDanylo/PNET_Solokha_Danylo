namespace PNET_Solokha_Danylo.Domain.Entities;

public class Medicine
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }
    public decimal BasePrice { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; } = true;

    public Category Category { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
