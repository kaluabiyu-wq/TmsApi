using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Api.Notifications;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[AllowAnonymous] // register, login and refresh must stay reachable once a fallback policy is added
public class AuthController : ControllerBase
{
    // Every self-registered account gets this role. Admin and Instructor are
    // assigned by an admin (or the bootstrap), never chosen by the caller.
    private const string DefaultRole = "Student";

    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly TmsDbContext _context;
    private readonly TokenService _tokenService;
    private readonly TmsApi.Api.Services.IPasswordResetEmailSender _resetEmail;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly RoleBasedNotificationService _roleNotifications;

    public AuthController(UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TmsDbContext context,
        TokenService tokenService,
        TmsApi.Api.Services.IPasswordResetEmailSender resetEmail,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        RoleBasedNotificationService roleNotifications
    )
    {
        _resetEmail = resetEmail;
        _configuration = configuration;
        _logger = logger;
        _roleNotifications = roleNotifications;
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    // No Role field: callers can no longer pick their own role.
    public record RegisterRequest(string Email, string Password,
     string FirstName, string LastName
    );

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Ok(new { message = "Registration request recieved." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        // Make sure the fixed role exists (the bootstrap normally creates it).
        if (!await _roleManager.RoleExistsAsync(DefaultRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(DefaultRole));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
        if (!roleResult.Succeeded)
        {
            // Don't leave a user with no role behind.
            await _userManager.DeleteAsync(user);
            return StatusCode(500, new { detail = "Registration failed." });
        }

        try
        {
            await _roleNotifications.NotifyAdminsAsync("New student registration",
                $"{user.FirstName} {user.LastName} registered a student account.",
                "Users", "Info", "/admin/users");
        }
        catch (Exception notificationError)
        {
            _logger.LogWarning(notificationError, "Could not notify administrators about a new student registration.");
        }
        return Ok(new { message = "Registration successful." });
    }


    public record LoginRequest(string Email, string Password);

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }
        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new { detail = "Account locked due to multiple failed login attempts. Try again in 15 minutes." });

        }
        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateJwt(user, roles);


        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token

        });
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await _context.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
        if (storedToken == null)
        {
            return Unauthorized(new { detail = "Invalid refresh token." });
        }
        if (storedToken.IsUsed)
        {
            var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == storedToken.UserId)
            .ToListAsync();
            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
            return Unauthorized(new { detail = "Token theft detected. All user sessions revoked." });

        }

        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { detail = "Refresh token expired or revoked." });
        }

        storedToken.IsUsed = true;

        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);

        var roles = await _userManager.GetRolesAsync(user!);
        var newAccessToken = _tokenService.GenerateJwt(user!, roles);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    public record ForgotPasswordRequest(string Email);

    /// <summary>
    /// Always answers 200 with the same message, whether or not the account exists,
    /// so the endpoint cannot be used to discover registered emails.
    /// </summary>
    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        const string message = "If an account with that email exists, a password reset link has been sent.";

        if (string.IsNullOrWhiteSpace(request.Email)) return Ok(new { message });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return Ok(new { message });

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var baseUrl = (_configuration["Frontend:BaseUrl"]
                           ?? _configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault()
                           ?? "http://localhost:4200").TrimEnd('/');
            var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            await _resetEmail.SendAsync(user.Email, user.FirstName, link, ct);
        }
        catch (Exception ex)
        {
            // Never reveal delivery problems to the caller; log them for the operator.
            _logger.LogError(ex, "Could not send password reset email.");
        }

        return Ok(new { message });
    }

    public record ResetPasswordRequest(string Email, string Token, string NewPassword);

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        const string invalid = "This reset link is invalid or has expired. Request a new one.";

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { detail = "Email, token and new password are required." });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return BadRequest(new { detail = invalid });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            // Password-rule failures are safe to show; token failures get the generic message.
            var ruleErrors = result.Errors
                .Where(e => e.Code.StartsWith("Password"))
                .Select(e => e.Description)
                .ToList();
            return ruleErrors.Count > 0
                ? BadRequest(new { errors = ruleErrors })
                : BadRequest(new { detail = invalid });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        // Sign out every existing session of this account.
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();
        foreach (var t in tokens) t.IsRevoked = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated. You can now sign in." });
    }
}