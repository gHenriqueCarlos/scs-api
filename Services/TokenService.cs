// Services/TokenService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ScspApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class TokenService
{
    private readonly IConfiguration _cfg;
    private readonly UserManager<User> _userManager;

    public TokenService(IConfiguration cfg, UserManager<User> userManager)
    { _cfg = cfg; _userManager = userManager; }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var baseClaims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("UserId", user.Id)
        };
        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        var claims = baseClaims.Concat(roleClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30), // curto
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken(int bytes = 32)
    {
        // base64url
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
