
using TmsApi.Application.DTOs;

using TmsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace TmsApi.Infrastructure.Persistence.Services;


public class CourseService(TmSDbContext context, ILogger<CourseService> logger) : ICourseService
{
 public Task<CourseResponseDto?> GetByCodeAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => new CourseResponseDto(
                c.Id, c.Code, c.Title, c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);

  public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
{

var course = new Course
{
Code = request.Code,
Title = request.Title,
MaxCapacity = request.MaxCapacity
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

}

