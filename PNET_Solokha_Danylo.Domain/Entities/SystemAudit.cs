namespace PNET_Solokha_Danylo.Domain.Entities;

public class SystemAudit
{
    public int LogId { get; set; }
    public DateTime LogDate { get; set; } = DateTime.Now;
    public string? Severity { get; set; } = "INFO";
    public string? ActionType { get; set; }
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public string? ColumnName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserName { get; set; }
    public string? AdditionalInfo { get; set; }
}
