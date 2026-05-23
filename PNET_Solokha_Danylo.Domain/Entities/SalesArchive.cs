namespace PNET_Solokha_Danylo.Domain.Entities;

public class SalesArchive
{
    public int ArchiveId { get; set; }
    public int? SaleId { get; set; }
    public int? MedicineId { get; set; }
    public int? Quantity { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? Reason { get; set; }
    public DateTime ArchivedAt { get; set; } = DateTime.Now;
}
