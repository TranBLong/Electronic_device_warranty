using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "RefreshToken không được để trống")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
