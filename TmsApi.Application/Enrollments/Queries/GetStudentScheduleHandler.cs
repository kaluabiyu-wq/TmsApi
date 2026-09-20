
using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetStudentSchedulerHandler(IEnrollmentService repo)
 : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(
        GetStudentScheduleQuery query,CancellationToken ct
    )
    {
        var enrollments = await repo.GetByStudentIdAsync(query.StudentId,ct);

        var items = enrollments.Select(e => new ScheduleItemDto
        (
         e.Course.Code,
         e.Course.Title,
         "TBD")).ToList();

        return new ScheduleDto(query.StudentId,items);
    }
}