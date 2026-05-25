namespace PNET_Solokha_Danylo.Domain.Entities;

public class Medicine
{
    private Medicine() { } // For EF Core

    public Medicine(string name, int categoryId, int supplierId, decimal basePrice)
    {
        Update(name, categoryId, supplierId, basePrice);
        TotalStock = 0;
        IsActive = true;
    }

    public int MedicineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int CategoryId { get; private set; }
    public int SupplierId { get; private set; }
    public decimal BasePrice { get; private set; }
    public int TotalStock { get; private set; }
    public bool IsActive { get; private set; }

    public Category Category { get; private set; } = null!;
    public Supplier Supplier { get; private set; } = null!;
    public ICollection<Inventory> Inventories { get; private set; } = new List<Inventory>();
    public ICollection<Sale> Sales { get; private set; } = new List<Sale>();

    public void Update(string name, int categoryId, int supplierId, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Medicine name cannot be empty", nameof(name));
        
        if (categoryId <= 0)
            throw new ArgumentException("Valid Category ID is required", nameof(categoryId));

        if (supplierId <= 0)
            throw new ArgumentException("Valid Supplier ID is required", nameof(supplierId));

        if (basePrice < 0)
            throw new ArgumentException("Base price cannot be negative", nameof(basePrice));

        Name = name;
        CategoryId = categoryId;
        SupplierId = supplierId;
        BasePrice = basePrice;
    }

    public void SetStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentException("Stock cannot be negative", nameof(stock));
        TotalStock = stock;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
