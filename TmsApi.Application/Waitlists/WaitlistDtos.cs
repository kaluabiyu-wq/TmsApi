using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.Waitlists;

public class JoinWaitlistRequest
{
    [Range(1, int.MaxValue)] public int StudentId { get; set; }
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
}

/// <param name="QueuePosition">1-based place among students still Waiting; null once offered/closed.</param>
public record WaitlistResponse(
    int Id,
    int StudentId,
    string StudentName,
    string RegistrationNumber,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    int? QueuePosition,
    string Status,
    DateTime RequestedAt,
    DateTime? OfferedAt,
    DateTime? ExpiresAt);