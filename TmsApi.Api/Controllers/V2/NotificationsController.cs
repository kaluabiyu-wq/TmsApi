using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Notifications;
using TmsApi.Api.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsController(INotificationService notifications, RoleBasedNotificationService roleNotifications) : ControllerBase
{
    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return Ok(await notifications.GetMineAsync(userId, page, pageSize, unreadOnly, ct));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return Ok(new { unreadCount = await notifications.GetUnreadCountAsync(userId, ct) });
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return await notifications.MarkAsReadAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return Ok(new { updated = await notifications.MarkAllAsReadAsync(userId, ct) });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return await notifications.DeleteAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpGet("{id:long}/delivery-status")]
    public async Task<IActionResult> GetDeliveryStatus(long id, CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return Ok(await notifications.GetDeliveryStatusAsync(userId, id, ct));
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        return Ok(await notifications.GetPreferencesAsync(userId, ct));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(UpdateNotificationPreferenceRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId; if (userId is null) return Unauthorized();
        try { return Ok(await notifications.UpdatePreferenceAsync(userId, request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    public sealed record AnnouncementRequest(string Title, string Message, string Role, string? Category = "Announcements", string? ActionUrl = null);

    [HttpPost("announcements")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest request, CancellationToken ct)
    {
        var role = request.Role?.Trim();
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 180 ||
            string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 4000)
            return BadRequest(new { message = "A title (max 180 characters) and message (max 4000 characters) are required." });
        if (role is not ("Student" or "Instructor" or "Admin" or "SuperAdmin"))
            return BadRequest(new { message = "Role must be Student, Instructor, Admin or SuperAdmin." });

        await roleNotifications.NotifyRoleAsync(role, request.Title, request.Message,
            string.IsNullOrWhiteSpace(request.Category) ? "Announcements" : request.Category,
            "Info", request.ActionUrl, ct);
        return Accepted(new { message = "Announcement notifications have been created for the selected role." });
    }
}
