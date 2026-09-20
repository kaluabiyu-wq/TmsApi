using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
 

namespace TmsApi.Infrastructure.Workers;
public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
       
        using var scope = scopeFactory.CreateScope();

     
        var svc = scope.ServiceProvider
                       .GetRequiredService<IEnrollmentService>();

        //   var enrollments = svc.GetAllAsync().Result;
        // foreach (var enrollment in enrollments)
        // {
        //     Console.WriteLine(
        //         $"Processing {enrollment.Id} - {enrollment.StudentId} - {enrollment.CourseCode}");
        // }
    }
}


