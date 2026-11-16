using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Waitlists;

namespace TmsApi.Application.Waitlists;

public interface IWaitlistService
{
    Task<Result<WaitlistResponse, WaitlistError>> JoinWaitlistAsync(JoinWaitlistRequest request, CancellationToken ct);
    Task<PagedResponse<WaitlistResponse>> GetCourseWaitlistAsync(int courseId, int page, int pageSize, CancellationToken ct);
    Task<List<WaitlistResponse>> GetStudentWaitlistAsync(int studentId, CancellationToken ct);

    /// <summary>Offers every currently free seat of the course to the next Waiting students.</summary>
    Task<Result<IReadOnlyList<WaitlistResponse>, WaitlistError>> OfferNextSeatAsync(int courseId, CancellationToken ct);
    /// <summary>Staff override: offer a free seat to one specific Waiting entry.</summary>
    Task<Result<WaitlistResponse, WaitlistError>> OfferSeatAsync(int entryId, CancellationToken ct);
    Task<Result<WaitlistResponse, WaitlistError>> AcceptOfferAsync(int entryId, CancellationToken ct);
    Task<Result<WaitlistResponse, WaitlistError>> CancelWaitlistAsync(int entryId, CancellationToken ct);

    /// <summary>Expires stale offers and re-offers their seats. Returns how many offers expired.</summary>
    Task<int> ExpireOffersAsync(CancellationToken ct);
    /// <summary>Offers free seats on every course that has students waiting (used by the background worker).</summary>
    Task ProcessOpenSeatsAsync(CancellationToken ct);
}

/// <summary>Application-side port; implemented with SignalR in the Api project.</summary>
public interface IWaitlistNotifier
{
    Task NotifyOfferedAsync(WaitlistResponse offer);
    Task NotifyOfferExpiredAsync(int studentId, int courseId, string courseTitle);
}