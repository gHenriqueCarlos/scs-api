using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScspApi.Data;
using ScspApi.Infrastructure;
using ScspApi.Models;
using ScspApi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ScspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ApiControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly OtpService _otpService;
        private readonly TokenService _tokenService;  
        private readonly ApplicationDbContext _db;  

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            EmailService emailService,
            OtpService otpService,
            TokenService tokenService,           
            ApplicationDbContext db)                
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
            _otpService = otpService;
            _tokenService = tokenService;              
            _db = db;                                  
        }

        // # REGISTRO / CONFIRMAÇÃO #

        [EnableRateLimiting("EmailSend")]
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<object>>> Register(
            [FromBody] RegisterModel model,
            [FromHeader(Name = "scsApp-Token")] string appToken)
        {
            var appValidationService = new AppValidationService();
            if (!appValidationService.IsValidToken(appToken))
                return ApiUnauthorized("Token do app inválido.");

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return ApiBadRequest("Falha no registro.", IdentityErrors(result));

            await _userManager.AddToRoleAsync(user, "User");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { token, email = user.Email }, Request.Scheme);

            var (code, entry) = await _otpService.IssueAsync(user, OtpPurpose.EmailConfirmation, TimeSpan.FromMinutes(15), 6);
            var minutes = (int)Math.Round((entry.ExpiresAt - DateTime.UtcNow).TotalMinutes);

            var html = EmailTemplates.EmailConfirmationLinkAndCode(
                user.FullName?.Split(' ').FirstOrDefault() ?? "",
                confirmationLink!,
                code,
                minutes);

            await _emailService.SendEmailAsync(user.Email, "Confirmação de E-mail", html);

            return ApiMessage("Usuário registrado com sucesso! Verifique seu e-mail para confirmar a conta.");
        }

        [EnableRateLimiting("EmailSend")]
        [HttpPost("request-email-confirmation-code")]
        public async Task<ActionResult<ApiResponse<object>>> RequestEmailConfirmationCode([FromBody] RequestCodeModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiMessage("Se o e-mail existir, você receberá um código.");

            if (user.EmailConfirmed) 
                return ApiMessage("E-mail já confirmado.");

            await _otpService.IssueAndSendAsync(user, OtpPurpose.EmailConfirmation, TimeSpan.FromMinutes(15), 6);
            return ApiMessage("Se o e-mail existir, você receberá um código para confirmação.");
        }

        [EnableRateLimiting("OtpVerify")]
        [HttpPost("confirm-email-with-code")]
        public async Task<ActionResult<ApiResponse<object>>> ConfirmEmailWithCode([FromBody] ConfirmEmailWithCodeModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiBadRequest("E-mail não encontrado.");

            if (user.EmailConfirmed) 
                return ApiMessage("E-mail já confirmado.");

            var (ok, err, entry) = await _otpService.ValidateAsync(user, OtpPurpose.EmailConfirmation, model.Code);
            if (!ok) 
                return ApiBadRequest(err!);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded) 
                return ApiBadRequest("Falha ao confirmar o e-mail.", IdentityErrors(result));

            await _otpService.ConsumeAsync(entry!);
            return ApiMessage("E-mail confirmado com sucesso!");
        }

        [HttpGet("confirm-email")]
        public async Task<ActionResult<ApiResponse<object>>> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return ApiBadRequest("Token ou e-mail inválidos.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) 
                return ApiBadRequest("Usuário não encontrado.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded) 
                return ApiBadRequest("Falha ao confirmar o e-mail.", IdentityErrors(result));

            return ApiMessage("E-mail confirmado com sucesso! Agora você pode fazer login.");
        }

        // # LOGIN / TOKENS #

        [EnableRateLimiting("AuthSensitive")]
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return ApiUnauthorized("Usuário não encontrado.", code: "USER_NOT_FOUND");

            if (!user.EmailConfirmed)
                return ApiUnauthorized("Por favor, confirme seu e-mail antes de fazer login.", code: "EMAIL_NOT_CONFIRMED");

            if (user.IsBanned)
                return ApiUnauthorized($"Usuário banido. Motivo: {user.BanReason}", code: "USER_BANNED");

            var signIn = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (!signIn.Succeeded)
                return ApiUnauthorized("Credenciais inválidas.", code: "INVALID_CREDENTIALS");

            var roles = await _userManager.GetRolesAsync(user);

            var access = await _tokenService.GenerateAccessTokenAsync(user);
            var refresh = TokenService.GenerateRefreshToken();

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refresh,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                DeviceId = model.DeviceId,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _db.SaveChangesAsync();

            var dto = new LoginResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                Token = access,
                RefreshToken = refresh              // para atualizar o token no app
            };

            return ApiOk((object)dto, "Login efetuado com sucesso.");
        }

        // Renova access token e ROTACIONA refresh
        [EnableRateLimiting("AuthSensitive")]
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<object>>> Refresh([FromBody] RefreshRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiBadRequest("Refresh token ausente.", code: "REFRESH_REQUIRED");

            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
            if (rt == null) 
                return ApiUnauthorized("Refresh token inválido.", code: "REFRESH_INVALID");

            if (rt.RevokedAt != null) 
                return ApiUnauthorized("Refresh token revogado.", code: "REFRESH_REVOKED");

            if (DateTime.UtcNow >= rt.ExpiresAt) 
                return ApiUnauthorized("Refresh token expirado.", code: "REFRESH_EXPIRED");

            var user = await _userManager.FindByIdAsync(rt.UserId);
            if (user == null) return ApiUnauthorized("Usuário não encontrado.", code: "USER_NOT_FOUND");

            if (!string.IsNullOrWhiteSpace(req.DeviceId) && !string.Equals(req.DeviceId, rt.DeviceId, StringComparison.Ordinal))
                return ApiUnauthorized("Dispositivo inválido.", code: "DEVICE_MISMATCH");

            // Remover o antigo e adicionar o novo
            var newRefresh = TokenService.GenerateRefreshToken();
            rt.RevokedAt = DateTime.UtcNow;
            rt.ReplacedByToken = newRefresh;

            var newRt = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefresh,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                DeviceId = rt.DeviceId,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _db.RefreshTokens.Add(newRt);

            var newAccess = await _tokenService.GenerateAccessTokenAsync(user);
            await _db.SaveChangesAsync();

            var payload = new RefreshResponse
            {
                Token = newAccess,
                RefreshToken = newRefresh
            };
            return ApiOk((object)payload, "Token renovado.");
        }

        // logout do dispositivo
        [Authorize]
        [EnableRateLimiting("AuthSensitive")]
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RevokeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiBadRequest("Refresh token ausente.");

            var userId = User.FindFirst("UserId")?.Value;
            if (userId is null) 
                return ApiUnauthorized("Usuário não autenticado.");

            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == req.RefreshToken);
            if (rt != null && rt.UserId == userId){
                rt.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return ApiMessage("Sessão encerrada.");
        }

        // # SENHA #

        [Authorize]
        [EnableRateLimiting("AuthSensitive")]
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
                return ApiUnauthorized("Usuário não autenticado.");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) 
                return ApiBadRequest("Não foi possível alterar a senha.", IdentityErrors(result));

            return ApiMessage("Senha alterada com sucesso!");
        }

        [EnableRateLimiting("EmailSend")]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return ApiBadRequest("E-mail não encontrado.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            var (code, entry) = await _otpService.IssueAsync(user, OtpPurpose.PasswordReset, TimeSpan.FromMinutes(15), 6);
            var minutes = (int)Math.Round((entry.ExpiresAt - DateTime.UtcNow).TotalMinutes);

            var html = EmailTemplates.PasswordResetLinkAndCode(
                user.FullName?.Split(' ').FirstOrDefault() ?? "",
                resetPasswordLink!,
                code,
                minutes);

            await _emailService.SendEmailAsync(user.Email, "Recuperação de Senha", html);
            return ApiMessage("Se o e-mail existir, enviaremos um link e um código para recuperação de senha.");
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiBadRequest("E-mail não encontrado.");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded) 
                return ApiBadRequest("Não foi possível redefinir a senha.", IdentityErrors(result));

            return ApiMessage("Senha redefinida com sucesso!");
        }

        [EnableRateLimiting("EmailSend")]
        [HttpPost("request-password-reset-code")]
        public async Task<ActionResult<ApiResponse<object>>> RequestPasswordResetCode([FromBody] RequestCodeModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiMessage("Se o e-mail existir, você receberá um código.");

            await _otpService.IssueAndSendAsync(user, OtpPurpose.PasswordReset, TimeSpan.FromMinutes(15), 6);
            return ApiMessage("Se o e-mail existir, você receberá um código para redefinir a senha.");
        }

        [EnableRateLimiting("OtpVerify")]
        [HttpPost("reset-password-with-code")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPasswordWithCode([FromBody] ResetPasswordWithCodeModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiBadRequest("E-mail não encontrado.");

            var (ok, err, entry) = await _otpService.ValidateAsync(user, OtpPurpose.PasswordReset, model.Code);
            if (!ok) 
                return ApiBadRequest(err!);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded) 
                return ApiBadRequest("Não foi possível redefinir a senha.", IdentityErrors(result));

            await _otpService.ConsumeAsync(entry!);
            await _userManager.UpdateSecurityStampAsync(user); // invalidar as sessoes
            return ApiMessage("Senha redefinida com sucesso!");
        }

        // # PERFIL / ROLES #

        [Authorize]
        [HttpGet("get-user-info")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserInfo()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null) 
                return ApiUnauthorized("Usuário não autenticado.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) 
                return ApiNotFound("Usuário não encontrado.");

            var userInfo = new { user.Id, user.FullName, user.Email };
            return ApiOk((object)userInfo, "Dados do usuário.");
        }

        // Nesse caso o CPF ou CNPJ servem apenas para validar um usuario "verificado".
        [Authorize]
        [HttpPut("update-cpfcnpj")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateCpfCnpj([FromBody] UpdateCpfCnpjModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
                return ApiUnauthorized("Usuário não autenticado.");

            if (string.IsNullOrEmpty(model.Cpf) && string.IsNullOrEmpty(model.Cnpj))
                return ApiBadRequest("Informe o CPF ou CNPJ para atualização.");

            user.Cpf = model.Cpf;
            user.Cnpj = model.Cnpj;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded) 
                return ApiBadRequest("Falha ao atualizar o perfil.", IdentityErrors(result));

            var isInRole = await _userManager.IsInRoleAsync(user, "Verified");
            if (!isInRole) await _userManager.AddToRoleAsync(user, "Verified");

            var msg = string.IsNullOrEmpty(model.Cpf) ? "CNPJ atualizado." : "CPF atualizado.";
            return ApiMessage(msg);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("add-role")]
        public async Task<ActionResult<ApiResponse<object>>> AddRole([FromBody] AddRoleModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) 
                return ApiBadRequest("Usuário não encontrado.");

            var isInRole = await _userManager.IsInRoleAsync(user, model.Role);
            if (isInRole) 
                return ApiBadRequest("Usuário já possui esse cargo.");

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (!result.Succeeded) 
                return ApiBadRequest("Não foi possível adicionar o cargo.", IdentityErrors(result));

            return ApiMessage($"Cargo {model.Role} adicionado com sucesso!");
        }

        //private async Task<string> GenerateJwtTokenAsync(User user)
        //{
        //    var baseClaims = new[]
        //    {
        //        new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? user.Email),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        new Claim("UserId", user.Id)
        //    };

        //    var roles = await _userManager.GetRolesAsync(user);
        //    var roleClaims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        //    var claims = baseClaims.Concat(roleClaims);

        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //    var token = new JwtSecurityToken(
        //        issuer: _configuration["Jwt:Issuer"],
        //        audience: _configuration["Jwt:Audience"],
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddMinutes(30),
        //        signingCredentials: creds
        //    );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}
    }
}
