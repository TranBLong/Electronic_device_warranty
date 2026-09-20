using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty; // Mã Serial / IMEI duy nhất

        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // Điện thoại, Tivi, Tủ lạnh...

        public DateTime PurchaseDate { get; set; }
        public DateTime WarrantyExpiryDate { get; set; }

        // Khách hàng sở hữu sản phẩm này
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        // Các lần bảo hành của sản phẩm này
        public ICollection<WarrantyTicket> WarrantyTickets { get; set; } = new List<WarrantyTicket>();
    }
}