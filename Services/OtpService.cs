using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScspApi.Data;
using ScspApi.Models;

namespace ScspApi.Services;

public class OtpService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly EmailService _emailService;

    public OtpService(ApplicationDbContext db, UserManager<User> userManager, EmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task IssueAndSendAsync(User user, OtpPurpose purpose, TimeSpan? ttl = null, int digits = 6)
    {
        // opcional: invalida anteriores não usados do mesmo propósito
        var old = await _db.OtpCodes.Where(x => x.UserId == user.Id && x.Purpose == purpose && x.ConsumedAt == null).ToListAsync();
        if (old.Count > 0) { _db.OtpCodes.RemoveRange(old); await _db.SaveChangesAsync(); }

        var code = OtpCrypto.GenerateNumericCode(digits);  // "493201"
        var salt = OtpCrypto.GenerateSalt();
        var hash = OtpCrypto.HashCode(code, salt);

        var entity = new OtpCode
        {
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = hash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(15)),
            MaxAttempts = 5
        };

        _db.OtpCodes.Add(entity);
        await _db.SaveChangesAsync();

        // Enviar e-mail 
        //        var subject = purpose == OtpPurpose.PasswordReset ? "Código para redefinir senha" : "Código para confirmar e-mail";
        //        var body = $@"
        //<p>Seu código é: <b style=""font-size:18px"">{code}</b></p>
        //<p>Ele expira em {(int)((entity.ExpiresAt - DateTime.UtcNow).TotalMinutes)} minutos.</p>
        //";
        //        await _emailService.SendEmailAsync(user.Email!, subject, body);

        var subject = purpose == OtpPurpose.PasswordReset
            ? "Código para redefinir senha"
            : "Código de confirmação de e-mail";

        var minutes = (int)Math.Round((entity.ExpiresAt - DateTime.UtcNow).TotalMinutes);

        string html = purpose == OtpPurpose.PasswordReset
            ? EmailTemplates.PasswordResetCode(user.FullName?.Split(' ').FirstOrDefault() ?? "", code, minutes)
            : EmailTemplates.EmailConfirmationCode(user.FullName?.Split(' ').FirstOrDefault() ?? "", code, minutes);

        await _emailService.SendEmailAsync(user.Email!, subject, html);
    }

    public async Task<(string code, OtpCode entry)> IssueAsync(User user, OtpPurpose purpose, TimeSpan? ttl = null, int digits = 6)
    {
        var old = await _db.OtpCodes
            .Where(x => x.UserId == user.Id && x.Purpose == purpose && x.ConsumedAt == null)
            .ToListAsync();
        if (old.Count > 0) { _db.OtpCodes.RemoveRange(old); await _db.SaveChangesAsync(); }

        var code = OtpCrypto.GenerateNumericCode(digits);
        var salt = OtpCrypto.GenerateSalt();
        var hash = OtpCrypto.HashCode(code, salt);

        var entity = new OtpCode
        {
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = hash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(15)),
            MaxAttempts = 5
        };

        _db.OtpCodes.Add(entity);
        await _db.SaveChangesAsync();

        return (code, entity);
    }
    public async Task<(bool ok, string? error, OtpCode? entry)> ValidateAsync(User user, OtpPurpose purpose, string code)
    {
        var otp = await _db.OtpCodes
            .Where(x => x.UserId == user.Id && x.Purpose == purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null) 
            return (false, "Nenhum código ativo. Solicite um novo.", null);

        if (DateTime.UtcNow > otp.ExpiresAt) 
            return (false, "Código expirado.", null);
        if (otp.Attempts >= otp.MaxAttempts) 
            return (false, "Muitas tentativas inválidas. Solicite um novo código.", null);

        var hash = OtpCrypto.HashCode(code, otp.Salt);
        var match = OtpCrypto.ConstantTimeEquals(hash, otp.CodeHash);

        if (!match)
        {
            otp.Attempts += 1;
            await _db.SaveChangesAsync();
            return (false, "Código inválido.", null);
        }

        return (true, null, otp);
    }

    public async Task ConsumeAsync(OtpCode otp)
    {
        otp.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
