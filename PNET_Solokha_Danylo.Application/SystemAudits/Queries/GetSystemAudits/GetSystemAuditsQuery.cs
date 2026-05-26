using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PNET_Solokha_Danylo.Application.Common.Interfaces;
using PNET_Solokha_Danylo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PNET_Solokha_Danylo.Application.SystemAudits.Queries.GetSystemAudits;

public record SystemAuditQueryResult(List<SystemAudit> Items, int TotalCount);

public record GetSystemAuditsQuery(
    string? SearchTerm = null,
    string? Severity = null,
    string? ActionType = null,
    string? TableName = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Skip = 0,
    int Take = 10
) : IRequest<SystemAuditQueryResult>;

public class GetSystemAuditsQueryHandler(
    IApplicationDbContextFactory contextFactory,
    ILogger<GetSystemAuditsQueryHandler> logger
) : IRequestHandler<GetSystemAuditsQuery, SystemAuditQueryResult>
{
    public async Task<SystemAuditQueryResult> Handle(GetSystemAuditsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling GetSystemAuditsQuery: SearchTerm={SearchTerm}, Severity={Severity}, ActionType={ActionType}, TableName={TableName}, StartDate={StartDate}, EndDate={EndDate}, Skip={Skip}, Take={Take}",
            request.SearchTerm, request.Severity, request.ActionType, request.TableName, request.StartDate, request.EndDate, request.Skip, request.Take);

        using var context = contextFactory.CreateDbContext();
        IQueryable<SystemAudit> baseQuery = context.SystemAudit;

        // Applying filters
        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            baseQuery = baseQuery.Where(x => x.Severity == request.Severity);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            baseQuery = baseQuery.Where(x => x.ActionType == request.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(request.TableName))
        {
            baseQuery = baseQuery.Where(x => x.TableName == request.TableName);
        }

        if (request.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.LogDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.LogDate <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            baseQuery = baseQuery.Where(x =>
                (x.TableName != null && x.TableName.ToLower().Contains(term)) ||
                (x.ColumnName != null && x.ColumnName.ToLower().Contains(term)) ||
                (x.OldValue != null && x.OldValue.ToLower().Contains(term)) ||
                (x.NewValue != null && x.NewValue.ToLower().Contains(term)) ||
                (x.UserName != null && x.UserName.ToLower().Contains(term)) ||
                (x.AdditionalInfo != null && x.AdditionalInfo.ToLower().Contains(term)));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(x => x.LogDate)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Successfully fetched {Count} (out of {Total}) SystemAudits.", items.Count, totalCount);
        return new SystemAuditQueryResult(items, totalCount);
    }
}
