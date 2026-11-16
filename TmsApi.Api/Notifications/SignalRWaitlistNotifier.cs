// using Microsoft.AspNetCore.SignalR;
// using TmsApi.Api.Hubs;
// using TmsApi.Application.Hubs;
// using TmsApi.Application.Waitlists;
// using TmsApi.Application.Waitlists.Interfaces;

// namespace TmsApi.Api.Notifications;

// public class SignalRWaitlistNotifier(IHubContext<TmsHub, ITmsHubClient> hub) : IWaitlistNotifier
// {
//     public Task NotifyOfferedAsync(WaitlistResponse o) =>
//         hub.Clients.Group(GroupNames.Student(o.StudentId.ToString()))
//             .ReceiveWaitlistOffer(o.CourseId, o.CourseTitle, o.ExpiresAt ?? DateTime.UtcNow);

//     public Task NotifyOfferExpiredAsync(int studentId, int courseId, string courseTitle) =>
//         hub.Clients.Group(GroupNames.Student(studentId.ToString()))
//             .ReceiveWaitlistOfferExpired(courseId, courseTitle);
// }