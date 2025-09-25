namespace ScspApi.Models
{
    public class LoginResponseDto
    {
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!; 
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = default!;
        public string? DeviceId { get; set; } 
    }

    public class RefreshResponse
    {
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }

    public class RevokeRequest
    {
        public string RefreshToken { get; set; } = default!;
    }

}
