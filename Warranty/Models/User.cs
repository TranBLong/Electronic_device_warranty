using Warranty.Enums;

namespace Warranty.Models;

public class User
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WarrantyCard> WarrantyCards { get; set; } = new List<WarrantyCard>();
    public ICollection<RepairRequest> CreatedRepairRequests { get; set; } = new List<RepairRequest>();
    public ICollection<RepairRequest> AssignedRepairRequests { get; set; } = new List<RepairRequest>();
    public ICollection<RepairRequestStatusHistory> RepairRequestStatusChanges { get; set; } = new List<RepairRequestStatusHistory>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
