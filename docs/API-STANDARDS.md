# TMS API standards (Phase 1)

One page. If a new feature breaks one of these rules, change the feature or change this page - do not silently diverge.
The reference implementation is **Classroom** (`ClassroomsController`, `ClassroomService`, `ClassroomDtos`).

## Request flow
Controller (thin) -> service interface (Application) -> service (Infrastructure) -> `TmsDbContext`.
Controllers do no queries and no business rules. Services return DTOs, never entities.
(MediatR handlers are used by existing course features; both styles are accepted, but do not mix them within one feature.)

## Routes and versions
- New endpoints are **v2**: `api/v{version:apiVersion}/<plural-lowercase-noun>`, `[ApiVersion("2.0")]`.
- v1 stays frozen for the current Angular screens. Do not change v1 response shapes.
- Actions: GET list, GET `{id:int}`, POST, PUT `{id:int}`. No verbs in URLs.

## DTOs
- `Create{Entity}Request`, `Update{Entity}Request`, `{Entity}Response`.
- Validate with DataAnnotations on the request DTO (`[ApiController]` turns failures into a 400 ProblemDetails automatically).
- Request DTOs never contain `Id`, `CreatedAt` or other server-owned fields.

## Errors (RFC 7807 ProblemDetails)
| Situation | Status | How |
|---|---|---|
| Invalid input | 400 | automatic from DataAnnotations |
| Not signed in | 401 | fallback policy |
| Wrong role | 403 | `[Authorize(Policy = ...)]` |
| Missing record | 404 | controller returns `NotFound()` when the service returns null |
| Duplicate / rule violation | 409 | service throws `InvalidOperationException` (mapped by `GlobalExceptionHandler`) |

## Lists and pagination
- Return the flat `PagedResponse<T>` (`items`, `totalCount`, `page`, `pageSize`, `totalPages`, ...).
- `page` default 1; `pageSize` default 20, **max 50** (clamp in the service).
- Always an explicit `OrderBy` before `Skip/Take`.

## Authorization
- Use the named policies in `Authorization/Policies.cs` (`AdminOnly`, `InstructorOrAdmin`) - not repeated role strings.
- Every endpoint already requires a signed-in user. Use `[AllowAnonymous]` only for register, login, refresh and health.
- Reads: the narrowest role that needs them. Writes: Admin unless a rule says otherwise.

## Dates and times
- Store and return **UTC**. `DateTime.UtcNow`, never `DateTime.Now`.
- Column type `timestamp with time zone`. JSON is ISO-8601.
- The browser converts to local time for display (Angular `date` pipe).

## Database naming and migrations
- Tables: plural PascalCase (`Classrooms`). Key: `Id`. Foreign key: `{Entity}Id`. Booleans: `Is...`. Timestamps: `...At`.
- Configure in an `IEntityTypeConfiguration<T>` class (picked up by `ApplyConfigurationsFromAssembly`), not with attributes on entities.
- One migration per feature, named verb + noun (`AddClassrooms`). **Generate it with `dotnet ef`, read it, then apply it.** Never edit a migration that has been applied anywhere.
- Reference data (rooms, categories) is **deactivated, not deleted** (`IsActive`), so history stays valid.
- People records use soft delete (`IsDeleted` + global query filter). Dependents of a soft-deletable parent carry the matching filter (see `EnrollmentConfiguration`). Use `IgnoreQueryFilters()` only for audit/admin views, and say why in a comment.

## Logging
- Structured templates (`"Created classroom {ClassroomId}"`), never string interpolation.
- Log decisions and state changes at Information, rejected input at Warning.
- Never log passwords, tokens, or personal data (email, phone, full names) .

## Tests
- Every new endpoint gets an API test for: 401, 403, happy path, 400, 404, and 409 where it applies.
- Use `TestAuth.SignInAs(role)`; use unique names (Guid suffix) so tests do not collide in the shared in-memory DB.

## Frontend
- Standalone components, signals, reactive forms, one service per API resource.
- New endpoints are called through `apiV2Url` (`services/api-url.ts`).
- Show API errors with `problemMessage(err)`, not hand-rolled `err.error.detail` checks.

## Known deviations (existing code, do not copy)
- `v2/students` and `v2/assessments` use a legacy `{ data, meta, links }` envelope.
- Mixed `ID` / `Id` property names in older entities.
- "Certficate" typos in file names and one route.
- `Enrollment.Status` is a free string.
- The v1 screens call `/api/v1`.
