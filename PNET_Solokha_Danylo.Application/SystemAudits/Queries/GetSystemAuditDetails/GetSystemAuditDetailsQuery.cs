using MediatR;
using Microsoft.EntityFrameworkCore;
using PNET_Solokha_Danylo.Domain.Entities;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.SystemAudits.Queries.GetSystemAuditDetails;

public class RelatedAuditDto
{
    public int LogId { get; set; }
    public DateTime LogDate { get; set; }
    public string? ActionType { get; set; }
    public string? ColumnName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserName { get; set; }
}

public class SystemAuditDetailsDto
{
    public int LogId { get; set; }
    public DateTime LogDate { get; set; }
    public string? Severity { get; set; }
    public string? ActionType { get; set; }
    public string? TableName { get; set; }
    public int? RecordId { get; set; }
    public string? ColumnName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserName { get; set; }
    public string? AdditionalInfo { get; set; }
    
    // Correlated history timeline
    public List<RelatedAuditDto> RelatedHistory { get; set; } = new();
}

public record GetSystemAuditDetailsQuery(int LogId) : IRequest<SystemAuditDetailsDto?>;

public class GetSystemAuditDetailsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSystemAuditDetailsQueryHandler> logger
) : IRequestHandler<GetSystemAuditDetailsQuery, SystemAuditDetailsDto?>
{
    public async Task<SystemAuditDetailsDto?> Handle(GetSystemAuditDetailsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling GetSystemAuditDetailsQuery for LogId={LogId}", request.LogId);
        
        using var context = contextFactory.CreateDbContext();
        
        var audit = await context.SystemAudit
            .FirstOrDefaultAsync(a => a.LogId == request.LogId, cancellationToken);
            
        if (audit == null)
        {
            logger.LogWarning("System audit log with ID {LogId} not found.", request.LogId);
            return null;
        }
        
        var relatedHistory = new List<RelatedAuditDto>();
        
        if (!string.IsNullOrEmpty(audit.TableName) && audit.RecordId.HasValue)
        {
            var related = await context.SystemAudit
                .Where(a => a.TableName == audit.TableName && a.RecordId == audit.RecordId && a.LogId != audit.LogId)
                .OrderByDescending(a => a.LogDate)
                .Take(15)
                .ToListAsync(cancellationToken);
                
            relatedHistory = related.Select(a => new RelatedAuditDto
            {
                LogId = a.LogId,
                LogDate = a.LogDate,
                ActionType = a.ActionType,
                ColumnName = a.ColumnName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                UserName = a.UserName
            }).ToList();
        }
        
        return new SystemAuditDetailsDto
        {
            LogId = audit.LogId,
            LogDate = audit.LogDate,
            Severity = audit.Severity,
            ActionType = audit.ActionType,
            TableName = audit.TableName,
            RecordId = audit.RecordId,
            ColumnName = audit.ColumnName,
            OldValue = audit.OldValue,
            NewValue = audit.NewValue,
            UserName = audit.UserName,
            AdditionalInfo = audit.AdditionalInfo,
            RelatedHistory = relatedHistory
        };
    }
}
