using System.ComponentModel.DataAnnotations;

namespace ScspApi.Models
{
    /*
     * 
     * Obrigado stackoverflow e pesquisas no google.
     * 
     */
    public enum OtpPurpose
    {
        EmailConfirmation = 1,
        PasswordReset = 2
    }
    public class OtpCode
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public OtpPurpose Purpose { get; set; }

        [Required]
        public string CodeHash { get; set; } = default!; // SHA-256 do código + salt

        [Required]
        public string Salt { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }  
        public DateTime? ConsumedAt { get; set; }  

        public int Attempts { get; set; } = 0;             // tentativas feitas
        public int MaxAttempts { get; set; } = 5;          // limite total
    }
}
