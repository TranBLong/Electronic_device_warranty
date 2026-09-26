using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class RepairRequestStatusHistory
    {
        public int Id { get; set; }

        public int RepairRequestId { get; set; }
        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest? RepairRequest { get; set; }

        [MaxLength(30)]
        public string? FromStatus { get; set; }

        [Required, MaxLength(30)]
        public string ToStatus { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Note { get; set; }

        public int ChangedByUserId { get; set; }
        [ForeignKey(nameof(ChangedByUserId))]
        public User? ChangedByUser { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}