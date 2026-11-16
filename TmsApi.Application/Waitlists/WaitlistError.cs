namespace TmsApi.Application.Waitlists;

public sealed record WaitlistError(string Code, string Message)
{
    public static WaitlistError CourseNotFound(int id) => new("course_not_found", $"Course {id} was not found.");
    public static WaitlistError StudentNotFound(int id) => new("student_not_found", $"Active student {id} was not found.");
    public static WaitlistError EntryNotFound(int id) => new("entry_not_found", $"Waitlist entry {id} was not found.");
    public static WaitlistError AlreadyEnrolled() => new("already_enrolled", "The student already has a pending or approved enrollment in this course.");
    public static WaitlistError AlreadyWaitlisted() => new("already_waitlisted", "The student is already on the waitlist for this course.");
    public static WaitlistError CourseHasSeats() => new("course_has_seats", "This course still has free seats. Enroll directly instead of joining the waitlist.");
    public static WaitlistError NoSeatAvailable() => new("no_seat_available", "There is no free seat to offer right now.");
    public static WaitlistError InvalidStatus(string status, string action) => new("invalid_status", $"Cannot {action}: the entry is '{status}'.");
    public static WaitlistError OfferExpired() => new("offer_expired", "This offer has expired. The seat was passed to the next student.");
}