# Phase 2 — Student Waitlist Management

## Implemented workflow

- A student can join a course waitlist only when the course has no unreserved seats.
- Active duplicate entries for the same student/course are prevented by a PostgreSQL partial unique index.
- Waiting entries are ordered by their queue position and request time.
- When a seat is available, the service changes the next waiting entry to `Offered` and reserves that seat for 24 hours.
- An accepted offer creates or reactivates an approved enrollment.
- A cancelled or expired offer releases its held seat; the maintenance worker offers it to the next eligible student.
- Waitlist notifications are sent through the `/hubs/waitlist` SignalR hub.
- Normal enrollment creation and waitlist seat allocation lock the course row and account for pending/approved enrollments plus active offers, preventing the normal enrollment endpoint from consuming a held waitlist seat.

## API (V2)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/v2/waitlists` | Join a full course waitlist |
| GET | `/api/v2/waitlists/student/{studentId}` | List a student's active entries |
| GET | `/api/v2/waitlists/course/{courseId}` | Staff queue view (paged) |
| POST | `/api/v2/waitlists/{id}/accept` | Accept an active offer |
| DELETE | `/api/v2/waitlists/{id}` | Leave a waiting list or decline an offer |
| POST | `/api/v2/waitlists/{id}/offer` | Staff: offer one entry a seat |
| POST | `/api/v2/waitlists/course/{courseId}/offer-next` | Staff: offer all currently free seats in queue order |

All endpoints require authentication. Course queue and staff offer endpoints require the `Instructor` or `Admin` policy.

## Database migration

The waitlist table and indexes are included in the existing EF Core migrations. From the `backend` directory, apply all pending migrations with:

```bash
dotnet ef database update \
  --project TmsInfrastructure/TmsApi.Infrastructure.csproj \
  --startup-project TmsApi/TmsApi.Api.csproj \
  --context TmsDbContext
```

The latest waitlist model synchronization migration is `20261116163050_SyncWaitlistModel`. Back up the database before applying migrations in a production environment.

## Frontend

The Angular route `/waitlists` provides student join/view/accept/leave controls and an Admin queue view. The page currently asks for the numeric Student ID and Course ID because the existing `Student` entity is not linked to `TmsUser` and the login token does not carry a Student ID claim.

## Important identity limitation

The existing project has no `Student.UserId` relationship. As in the existing enrollment flow, the current API accepts a `studentId` supplied by the authenticated client. This is a compatibility limitation, not strong per-student ownership enforcement. Before production use, link each Student record to its Identity user and derive the student ID from the authenticated principal for join, list, accept, and cancel operations. The SignalR hub's `JoinStudentGroup(studentId)` should be restricted using the same ownership mapping.

## Verification environment

The source archive targets .NET 10 (`net10.0`) and Angular 22. Angular 22 requires a supported current Node 22 release (22.22.3 or later). Run backend tests with `dotnet test TmsApi.slnx` and build the frontend with `npm ci && npm run build` from `Frontend`. These commands must be run in an environment with the required SDKs and package dependencies installed.
