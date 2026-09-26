using EWarrantySystem.DTOs;

namespace EWarrantySystem.Services
{
    public interface IRepairRequestService
    {
        Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> CreateAsync(
            RepairRequestCreateDto request);

        Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> AssignTechnicianAsync(
            int id, 
            RepairRequestAssignDto request,
            int changedByUserId);

        Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> UpdateStatusAsync(
            int id, 
            RepairRequestUpdateStatusDto request,
            int changedByUserId);

        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}