using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Authorization;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// Phase 1 template controller. Copy this shape for every new feature:
/// thin controller -> service interface -> DbContext, DTOs in and out,
/// PagedResponse for lists, named authorization policies,
/// and errors left to the global ProblemDetails handler.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/classrooms")]
[ApiVersion("2.0")]
public class ClassroomsController(IClassroomService classrooms) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<PagedResponse<ClassroomResponse>>> GetClassrooms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        return Ok(await classrooms.GetPageAsync(page, pageSize, isActive, ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<ClassroomResponse>> GetClassroom(int id, CancellationToken ct)
    {
        var classroom = await classrooms.GetByIdAsync(id, ct);
        return classroom is null ? NotFound() : Ok(classroom);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ClassroomResponse>> CreateClassroom(
        [FromBody] CreateClassroomRequest request,
        CancellationToken ct)
    {
        var created = await classrooms.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetClassroom), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ClassroomResponse>> UpdateClassroom(
        int id,
        [FromBody] UpdateClassroomRequest request,
        CancellationToken ct)
    {
        var updated = await classrooms.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
