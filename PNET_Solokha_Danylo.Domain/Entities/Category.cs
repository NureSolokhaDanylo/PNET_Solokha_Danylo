namespace PNET_Solokha_Danylo.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
