using System.ComponentModel.DataAnnotations;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    public string Token { get; set; } = default!;    

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }       
    public string? DeviceId { get; set; }           
    public string? Ip { get; set; }  

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
