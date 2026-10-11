using Microsoft.AspNetCore.Identity;
using TmsApi.Domain.Entities;


public static class AdminBootstrap
{
    private static readonly string[] Roles = ["Admin", "Instructor", "Student"];

    public static async Task RunAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TmsUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminBootstrap");

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var email = app.Configuration["Bootstrap:AdminEmail"];
        var password = app.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return; // already created on an earlier start
        }

        var admin = new TmsUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };

        var created = await userManager.CreateAsync(admin, password);
        if (!created.Succeeded)
        {
            // Never log the password; the errors only describe which rule failed.
            throw new InvalidOperationException(
                "Admin bootstrap failed: " +
                string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, "Admin");
        logger.LogInformation("Bootstrap admin {Email} created.", email);
    }
}