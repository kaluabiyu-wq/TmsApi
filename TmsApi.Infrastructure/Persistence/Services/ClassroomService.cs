using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Services;

public class ClassroomService(TmsDbContext context, ILogger<ClassroomService> logger) : IClassroomService
{
    public async Task<PagedResponse<ClassroomResponse>> GetPageAsync(
        int page, int pageSize, bool? isActive, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = context.Classrooms.AsNoTracking();

        if (isActive.HasValue)
        {
            var wanted = isActive.Value;
            query = query.Where(c => c.IsActive == wanted);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClassroomResponse(c.Id, c.Name, c.Location, c.Capacity, c.IsActive, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ClassroomResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ClassroomResponse?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Classrooms
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClassroomResponse(c.Id, c.Name, c.Location, c.Capacity, c.IsActive, c.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<ClassroomResponse> CreateAsync(CreateClassroomRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await NameExistsAsync(name, excludeId: null, ct))
        {
            logger.LogWarning("Rejected duplicate classroom name {ClassroomName}", name);
            throw new InvalidOperationException($"A classroom named '{name}' already exists.");
        }

        var classroom = new Classroom
        {
            Name = name,
            Location = request.Location.Trim(),
            Capacity = request.Capacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created classroom {ClassroomId} ({ClassroomName}, capacity {Capacity})",
            classroom.Id, classroom.Name, classroom.Capacity);

        return ToResponse(classroom);
    }

    public async Task<ClassroomResponse?> UpdateAsync(int id, UpdateClassroomRequest request, CancellationToken ct)
    {
        var classroom = await context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (classroom is null)
        {
            return null;
        }

        var name = request.Name.Trim();

        if (await NameExistsAsync(name, excludeId: id, ct))
        {
            logger.LogWarning(
                "Rejected rename of classroom {ClassroomId} to duplicate name {ClassroomName}", id, name);
            throw new InvalidOperationException($"A classroom named '{name}' already exists.");
        }

        classroom.Name = name;
        classroom.Location = request.Location.Trim();
        classroom.Capacity = request.Capacity;
        classroom.IsActive = request.IsActive;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated classroom {ClassroomId} (active: {IsActive})", classroom.Id, classroom.IsActive);

        return ToResponse(classroom);
    }

    private Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        // Case-insensitive: "Lab 1" and "lab 1" are the same room.
        var lowered = name.ToLower();

        return context.Classrooms.AnyAsync(
            c => c.Name.ToLower() == lowered && (excludeId == null || c.Id != excludeId),
            ct);
    }

    private static ClassroomResponse ToResponse(Classroom c) =>
        new(c.Id, c.Name, c.Location, c.Capacity, c.IsActive, c.CreatedAt);
}
