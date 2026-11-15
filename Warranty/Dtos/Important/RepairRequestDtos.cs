using System.ComponentModel.DataAnnotations;
using Warranty.Enums;
using System.Collections.Generic;

namespace Warranty.Dtos;

public sealed class CreateRepairRequest
{
    [Range(1, int.MaxValue)]
    public int WarrantyCardId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ServiceCenterId { get; init; }

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

public sealed record RepairRequestChargeabilityDto(
    bool IsChargeable,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal LaborCost);

public sealed class UpdateRepairRequestDetailsDto
{
    [StringLength(1000)]
    public string? DeviceCondition { get; init; }

    [StringLength(4000)]
    public string? DiagnosisNote { get; init; }

    [StringLength(4000)]
    public string? Resolution { get; init; }

    public DateTime? EstimatedCompletionDate { get; init; }
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
    int? ServiceCenterId,
    bool IsChargeable,
    decimal LaborCost,
    string? DeviceCondition,
    string? DiagnosisNote,
    string? Resolution,
    DateTime? EstimatedCompletionDate,
    IReadOnlyList<RepairStatusHistoryResponse> StatusHistory);

public sealed record DashboardSummaryResponse(
    int TotalWarrantyCards,
    int ActiveWarrantyCards,
    int TotalRepairRequests,
    int ReceivedRepairRequests,
    int InProgressRepairRequests,
    int CompletedRepairRequests);

public sealed record MonthlyRevenueItem(int Year, int Month, decimal TotalAmount);
public sealed record RevenueReportResponse(IReadOnlyList<MonthlyRevenueItem> Items, decimal GrandTotal);
public sealed record TechnicianPerformanceItem(
    int TechnicianId, string TechnicianName, int AssignedCount, int CompletedCount, double? AvgHandlingHours);
public sealed record TechnicianPerformanceResponse(IReadOnlyList<TechnicianPerformanceItem> Items);
public sealed record TopPartItem(int PartId, string PartNumber, string Name, int TotalQuantity, decimal TotalAmount);
public sealed record TopPartsReportResponse(IReadOnlyList<TopPartItem> Items);
public sealed record LowStockPartItem(int PartId, string PartNumber, string Name, int StockQuantity);
public sealed record LowStockReportResponse(IReadOnlyList<LowStockPartItem> Items);
public sealed record FeedbackRatingItem(int Rating, int Count);
public sealed record FeedbackReportResponse(
    double? AverageRating, int TotalCount, IReadOnlyList<FeedbackRatingItem> ByRating);
