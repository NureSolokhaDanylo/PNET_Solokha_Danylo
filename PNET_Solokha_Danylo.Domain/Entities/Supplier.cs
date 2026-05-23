namespace PNET_Solokha_Danylo.Domain.Entities;

public class Supplier
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime LastAuditDate { get; set; } = DateTime.Now;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
