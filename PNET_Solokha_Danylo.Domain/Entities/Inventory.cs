namespace PNET_Solokha_Danylo.Domain.Entities;

public class Inventory
{
    public int InventoryId { get; set; }
    public int MedicineId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public string? Location { get; set; } = "Main Shelf";

    public Medicine Medicine { get; set; } = null!;
}
