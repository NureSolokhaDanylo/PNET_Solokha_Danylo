namespace PNET_Solokha_Danylo.Domain.Entities;

public class Sale
{
    public int SaleId { get; set; }
    public int MedicineId { get; set; }
    public int Quantity { get; set; }
    public decimal SoldPrice { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal Discount { get; set; }

    public Medicine Medicine { get; set; } = null!;
}
