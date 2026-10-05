namespace Warranty.Models;

public class ServiceCenter
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Staff { get; set; } = new List<User>();
    public ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>();
}
