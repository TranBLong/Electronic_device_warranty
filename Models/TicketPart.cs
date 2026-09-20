using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class TicketPart
    {
        public int Id { get; set; }

        public int WarrantyTicketId { get; set; }
        [ForeignKey(nameof(WarrantyTicketId))]
        public WarrantyTicket? WarrantyTicket { get; set; }

        public int ReplacementPartId { get; set; }
        [ForeignKey(nameof(ReplacementPartId))]
        public ReplacementPart? ReplacementPart { get; set; }

        public int Quantity { get; set; } = 1; // Số lượng linh kiện dùng

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Giá linh kiện tại thời điểm sửa
    }
}