namespace PNET_Solokha_Danylo.Domain.Entities;

public class Category
{
    private Category() { } // For EF Core

    public Category(string name, string? description = null)
    {
        Update(name, description);
    }

    public int CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<Medicine> Medicines { get; private set; } = new List<Medicine>();

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));
        
        Name = name;
        Description = description;
    }
}
