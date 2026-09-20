using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class ReplacementPart
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string PartCode { get; set; } = string.Empty; // Mã linh kiện

        [Required, MaxLength(150)]
        public string PartName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; } // Số lượng tồn kho

        public ICollection<TicketPart> TicketParts { get; set; } = new List<TicketPart>();
    }
}