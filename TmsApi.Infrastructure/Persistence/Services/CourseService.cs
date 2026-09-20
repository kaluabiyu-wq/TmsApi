
using TmsApi.Application.DTOs;

using TmsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Courses.Commands;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TmsApi.Infrastructure.Persistence.Services;


public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
 public Task<CourseResponseDto?> GetByCodeAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

  public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request,string instructorId, CancellationToken ct)
{

var course = new Course
{
Code = request.Code,
Title = request.Title,
MaxCapacity = request.MaxCapacity,
InstructorId = instructorId
};
context.Courses.Add(course);
await context.SaveChangesAsync(ct);
logger.LogInformation("Created course {courseCode} ({Code})", course.Id, course.Code);
 return (await GetByCodeAsync(course.Code,ct))!;
}
public Task<bool> CodeExistAsync(string code, CancellationToken ct) =>
context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);


public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
    PagedRequest request, CancellationToken ct)
{
    IQueryable<Course> query = context.Courses.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c => EF.Functions.ILike(c.Title, $"%{request.Search}%")
                               || EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    IOrderedQueryable<Course> sortedQuery = request.OrderBy switch
    {
        "Code" => request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),
        "MaxCapacity" => request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),
        _ => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };

    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(c.Id,
         c.Code,
        c.Title,
        c.MaxCapacity, c.Enrollments.Count))
        .ToListAsync(ct);

    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}

public async Task<List<CourseResponseDto>> GetAllAsync(CancellationToken ct)
{
    return await context.Courses.AsNoTracking()
        .Select(c => new CourseResponseDto(
            c.Id, c.Code, c.Title, c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);
}
public async Task<CourseResponseDto> UpdateAsync(UpdateCourseCommand command, CancellationToken ct)
    {
       var course = await context.Courses
        .FirstOrDefaultAsync(c => c.Id == command.Id, ct)
        ?? throw new KeyNotFoundException($"Course {command.Id} not found");

    course.Title = command.Title;
    await context.SaveChangesAsync(ct);

    return new CourseResponseDto(
        course.Id, course.Code, course.Title, course.MaxCapacity, course.Enrollments.Count );
    }

public async Task DeleteAsync(int id, CancellationToken ct)
{
    var course = await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new KeyNotFoundException($"Course {id} not found");

    if (course.Enrollments.Any(e => e.Status != "Cancelled"))
        throw new InvalidOperationException("Cannot delete course: active student enrollments exist.");

    context.Courses.Remove(course);
    await context.SaveChangesAsync(ct);
}

}

