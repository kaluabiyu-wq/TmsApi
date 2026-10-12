using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;


[ApiController]
[Route("api/v{version:apiVersion}/account")]
[ApiVersion("2.0")]
public class AccountController(
    UserManager<TmsUser> userManager,
    TmsDbContext context) : ControllerBase
{
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            // Identity's messages describe which rule failed; they never echo the password.
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // End every existing session so a stolen refresh token stops working.
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
        }
        await context.SaveChangesAsync();

        return NoContent();
    }
}