using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Api.Authorization;

/// <summary>
/// Named authorization policies. New endpoints use [Authorize(Policy = Policies.X)]
/// instead of repeating role strings such as Roles = "Instructor,Admin".
/// Every endpoint already requires a signed-in user (the fallback policy in Program.cs);
/// these policies add the role requirement on top.
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string InstructorOrAdmin = "InstructorOrAdmin";

    public static AuthorizationBuilder AddTmsPolicies(this AuthorizationBuilder builder) =>
        builder
            .AddPolicy(AdminOnly, policy => policy.RequireRole("Admin"))
            .AddPolicy(InstructorOrAdmin, policy => policy.RequireRole("Instructor", "Admin"));
}
