using System.ComponentModel.DataAnnotations;
using Warranty.Enums;

namespace Warranty.Dtos;

public sealed class CreateRepairRequest
{
    [Range(1, int.MaxValue)]
    public int WarrantyCardId { get; init; }

    [Required, StringLength(4000)]
    public string Description { get; init; } = string.Empty;
}

public sealed class AssignTechnicianRequest
{
    [Range(1, int.MaxValue)]
    public int TechnicianId { get; init; }
}

public sealed class ChangeRepairStatusRequest
{
    [EnumDataType(typeof(RepairRequestStatus))]
    public RepairRequestStatus Status { get; init; }
}

public sealed record RepairStatusHistoryResponse(
    int Id,
    RepairRequestStatus? OldStatus,
    RepairRequestStatus NewStatus,
    int ChangedBy,
    DateTime ChangedAt);

public sealed record RepairRequestResponse(
    int Id,
    int WarrantyCardId,
    int CustomerId,
    string CustomerName,
    int? ReceptionistId,
    int? TechnicianId,
    string? TechnicianName,
    string Description,
    RepairRequestStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<RepairStatusHistoryResponse> StatusHistory);

public sealed record DashboardSummaryResponse(
    int TotalWarrantyCards,
    int ActiveWarrantyCards,
    int TotalRepairRequests,
    int ReceivedRepairRequests,
    int InProgressRepairRequests,
    int CompletedRepairRequests);
