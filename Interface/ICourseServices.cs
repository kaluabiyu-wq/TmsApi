


using TmsApi.Data;
using TmsApi.Entities;

public interface ICourseServices
{

 Task<Course?> GetByIdAsync(int id, CancellationToken ct);
    Task<Course> CreateAsync(Course course, CancellationToken ct);


}