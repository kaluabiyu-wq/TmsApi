using MediatR;
using TmsApi.Application.Common;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public record EnrollstudentCommand(int StudentId, string Coursecode)
: IRequest<Result<EnrollmentCreated,EnrollmentError>>;

public record EnrollmentCreated(int EnrollmentId,int StudentId, string CourseCode);

