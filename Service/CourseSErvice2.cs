using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

public class CourseServices(TmSDbContext context, ILogger<CourseServices> logger) : ICourseServices
{
  public async Task<Course?> GetByIdAsync(int id, CancellationToken ct)
{
     return await context.Courses.AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == id, ct);

throw new NotImplementedException();
}
public async Task<Course> CreateAsync(Course course, CancellationToken ct)
{
    context.Courses.Add(course);
    await context.SaveChangesAsync(ct);


    return course;

throw new NotImplementedException();
}

}