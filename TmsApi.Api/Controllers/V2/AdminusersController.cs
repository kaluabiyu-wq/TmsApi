using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;


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

    public record CreateStudentRequest(string Email, string Password, string FirstName, string LastName, string? RegistrationNumber);

    /// <summary>
    /// Admin-only creation of a Student login plus its linked Student record
    /// (so the new student can immediately enroll, take assessments and see grades).
    /// </summary>
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { detail = "First name, last name, email and password are required." });
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(new { detail = "A user with this email already exists." });
        }

        var regNo = request.RegistrationNumber?.Trim();
        if (string.IsNullOrEmpty(regNo))
        {
            var existing = await context.Students.Select(st => st.RegistrationNumber).ToListAsync(ct);
            var max = existing
                .Select(r => r.StartsWith("STU-") && int.TryParse(r.AsSpan(4), out var n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            regNo = $"STU-{max + 1:D4}";
        }
        else if (await context.Students.AnyAsync(st => st.RegistrationNumber == regNo, ct))
        {
            return Conflict(new { detail = "That registration number is already in use." });
        }

        const string role = "Student";
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleCreated = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleCreated.Succeeded)
            {
                return StatusCode(500, new { detail = "Could not create the Student role." });
            }
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });
        }

        var added = await userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return StatusCode(500, new { detail = "Could not assign the Student role." });
        }

        try
        {
            var student = new Student
            {
                RegistrationNumber = regNo,
                Name = $"{user.FirstName} {user.LastName}",
                UserId = user.Id,
                IsActive = true,
                Status = "Active"
            };
            context.Students.Add(student);
            await context.SaveChangesAsync(ct);

            return Created(string.Empty, new { id = user.Id, studentId = student.ID, email = user.Email, registrationNumber = regNo, role });
        }
        catch
        {
            // Never leave a login without its student record.
            await userManager.DeleteAsync(user);
            return StatusCode(500, new { detail = "Could not create the student record." });
        }
    }

    public record CreateInstructorRequest(string Email, string Password, string FirstName, string LastName, string? Department);

    /// <summary>
    /// Admin-only creation of an Instructor account. Instructors never self-register.
    /// </summary>
    [HttpPost("instructors")]
    public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { detail = "Email and password are required." });
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(new { detail = "A user with this email already exists." });
        }

        const string role = "Instructor";
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleCreated = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleCreated.Succeeded)
            {
                return StatusCode(500, new { detail = "Could not create the Instructor role." });
            }
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Department = request.Department
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });
        }

        var added = await userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded)
        {
            // Never leave an account without its role behind.
            await userManager.DeleteAsync(user);
            return StatusCode(500, new { detail = "Could not assign the Instructor role." });
        }

        return Created(string.Empty, new { id = user.Id, email = user.Email, role });
    }
}