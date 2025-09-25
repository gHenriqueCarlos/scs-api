using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScspApi.Data;
using ScspApi.Models;
using ScspApi.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Identity + EF Stores + Tokens
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        // Password
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.AllowedForNewUsers = true;

        // User
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key não configurado em appsettings.");
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = true;        // em dev, pode desativar se necessário
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) 
        };
    });

builder.Services.Configure<FormOptions>(options =>
{
    // 10 * 1024 * 1024; Limite de 10MB
    options.ValueLengthLimit = 10 * 1024 * 1024; 
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; 
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global por IP: 100 req / 1 min
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // AuthSensitive: 10 req / 1 min
    options.AddPolicy("AuthSensitive", httpContext =>
    {
        var key = "auth:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // EmailSend: 3 req / 2 min
    options.AddPolicy("EmailSend", httpContext =>
    {
        var key = "email:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(2),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    // OtpVerify: 8 req / 5 min
    options.AddPolicy("OtpVerify", httpContext =>
    {
        var key = "otp:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Controllers
builder.Services.AddControllers();

// Email 
builder.Services.AddSingleton<EmailService>();

// Demais serviços da app
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<TokenService>();

// CORS do app móvel 
// builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
//     .AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Se quiser OpenAPI/Swagger em dev, reative as linhas abaixo e ajuste o AddOpenApi()
    //  app.MapOpenApi();
}
app.UseStaticFiles();
app.UseHttpsRedirection();

// app.UseCors(); // se habilitar CORS acima

app.UseRateLimiter(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // var db = services.GetRequiredService<ApplicationDbContext>();
    // await db.Database.MigrateAsync();

    await SeedRolesService.SeedRolesAsync(services);
}

app.Run();
