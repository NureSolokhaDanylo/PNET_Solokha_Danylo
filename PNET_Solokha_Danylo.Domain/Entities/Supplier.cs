namespace PNET_Solokha_Danylo.Domain.Entities;

public class Supplier
{
    private Supplier() { } // For EF Core

    public Supplier(string name, string country, string? notes = null)
    {
        Update(name, country, notes);
        LastAuditDate = DateTime.Now;
    }

    public int SupplierId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime LastAuditDate { get; private set; }

    public ICollection<Medicine> Medicines { get; private set; } = new List<Medicine>();

    public void Update(string name, string country, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Supplier name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        Name = name;
        Country = country;
        Notes = notes;
    }

    public void MarkAuditCompleted()
    {
        LastAuditDate = DateTime.Now;
    }
}
