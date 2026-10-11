using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// Admin-only user management. Self-registration only ever creates Students,
/// so this is how someone becomes an Instructor or another Admin.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[ApiVersion("2.0")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(
    UserManager<TmsUser> userManager,
    RoleManager<IdentityRole> roleManager,
    TmsDbContext context) : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Admin", "Instructor", "Student"];

    public record SetRoleRequest(string Email, string Role);

    [HttpPut("role")]
    public async Task<IActionResult> SetRole([FromBody] SetRoleRequest request)
    {
        var role = AllowedRoles.FirstOrDefault(r =>
            string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return BadRequest(new { detail = $"Role must be one of: {string.Join(", ", AllowedRoles)}." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return NotFound(new { detail = "User not found." });
        }

        // An admin cannot change their own role: prevents locking the system
        // out of its last admin by accident.
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return BadRequest(new { detail = "You cannot change your own role." });
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == role)
        {
            return Ok(new { email = user.Email, role });
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Add the new role first so the user is never left without one.
        if (!currentRoles.Contains(role))
        {
            var added = await userManager.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                return StatusCode(500, new { detail = "Could not change role." });
            }
        }

        var toRemove = currentRoles.Where(r => r != role).ToList();
        if (toRemove.Count > 0)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded)
            {
                return StatusCode(500, new { detail = "Could not change role." });
            }
        }

        // Force a fresh login so the old role does not live on in refresh tokens.
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
        }
        await context.SaveChangesAsync();

        return Ok(new { email = user.Email, role });
    }
}