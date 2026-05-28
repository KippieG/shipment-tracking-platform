using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.Application.Features.Shipments.Queries.GetShipments;

public sealed record GetShipmentsQuery(
    ShipmentStatus? StatusFilter = null,
    ShipmentPriority? PriorityFilter = null,
    string? SearchTerm = null,
    string? CreatedBy = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ShipmentSummaryDto>>;

public sealed record ShipmentSummaryDto(
    Guid Id,
    string TrackingNumber,
    string SenderName,
    string RecipientName,
    string RecipientAddress,
    ShipmentStatus Status,
    string StatusLabel,
    ShipmentPriority Priority,
    decimal WeightKg,
    DateTime CreatedAt,
    DateTime? EstimatedDeliveryDate,
    int DocumentCount);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

public sealed class GetShipmentsHandler(IShipmentRepository repository)
    : IRequestHandler<GetShipmentsQuery, PagedResult<ShipmentSummaryDto>>
{
    public async Task<PagedResult<ShipmentSummaryDto>> Handle(
        GetShipmentsQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await repository.GetPagedAsync(
            query.StatusFilter, query.PriorityFilter,
            query.SearchTerm, query.CreatedBy,
            query.FromDate, query.ToDate,
            query.Page, query.PageSize, ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var dtos = items.Select(s => new ShipmentSummaryDto(
            s.Id, s.TrackingNumber, s.SenderName, s.RecipientName,
            s.RecipientAddress, s.Status, s.Status.ToString(), s.Priority,
            s.WeightKg, s.CreatedAt, s.EstimatedDeliveryDate,
            s.Documents.Count)).ToList();

        return new PagedResult<ShipmentSummaryDto>(
            dtos, totalCount, query.Page, query.PageSize, totalPages,
            query.Page < totalPages, query.Page > 1);
    }
}
