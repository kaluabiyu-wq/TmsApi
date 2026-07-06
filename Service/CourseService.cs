using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tms.Api.Dtos;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Services;

public class CourseService(TmSDbContext context, ILogger<CourseService> logger) : ICourseService
{
public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Courses.AsNoTracking()
.Where(c => c.Id == id)
.Select(c => new CourseResponseDto(
c.Id,c.Code,c.Title,c.MaxCapacity,
c.Enrollments.Count)).FirstOrDefaultAsync(ct);

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
logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
 return (await GetByIdAsync(course.Id,ct))!;
}
public Task<bool> CodeExistAsync(string code, CancellationToken ct) =>
context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);


}