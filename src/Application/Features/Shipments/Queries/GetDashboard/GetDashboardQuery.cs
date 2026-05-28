using MediatR;
using ShipmentTracking.Application.Common.Interfaces;
using ShipmentTracking.Domain.Enums;

namespace ShipmentTracking.Application.Features.Shipments.Queries.GetDashboard;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed record DashboardDto(
    int TotalShipments,
    int ActiveShipments,
    int OverdueShipments,
    Dictionary<string, int> StatusBreakdown,
    IReadOnlyList<RecentActivityDto> RecentActivity);

public sealed record RecentActivityDto(
    string TrackingNumber,
    string RecipientName,
    string Status,
    DateTime UpdatedAt);

public sealed class GetDashboardHandler(IShipmentRepository repository)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var statusCounts = await repository.GetStatusCountsAsync(ct);
        var overdue = await repository.GetOverdueAsync(ct);
        var (recent, _) = await repository.GetPagedAsync(null, null, null, null, null, null, 1, 10, ct);

        var activeStatuses = new[]
        {
            ShipmentStatus.Confirmed, ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery
        };

        return new DashboardDto(
            TotalShipments: statusCounts.Values.Sum(),
            ActiveShipments: activeStatuses.Sum(s => statusCounts.GetValueOrDefault(s, 0)),
            OverdueShipments: overdue.Count,
            StatusBreakdown: statusCounts.ToDictionary(k => k.Key.ToString(), v => v.Value),
            RecentActivity: recent.Select(s => new RecentActivityDto(
                s.TrackingNumber, s.RecipientName, s.Status.ToString(), s.UpdatedAt)).ToList());
    }
}
