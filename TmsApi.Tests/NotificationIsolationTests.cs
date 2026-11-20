using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi_Tests;

public sealed class NotificationIsolationTests : IClassFixture<CustomWebApplicatonFactory>
{
    private readonly CustomWebApplicatonFactory _factory;

    public NotificationIsolationTests(CustomWebApplicatonFactory factory) => _factory = factory;

    [Fact]
    public async Task UserCanOnlyListAndMarkTheirOwnNotifications()
    {
        var userA = $"notification-user-a-{Guid.NewGuid():N}";
        var userB = $"notification-user-b-{Guid.NewGuid():N}";
        long ownId;
        long otherId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
            var own = new Notification { UserId = userA, Title = "Own", Message = "Own message", Category = "General" };
            var other = new Notification { UserId = userB, Title = "Other", Message = "Other message", Category = "General" };
            db.Notifications.AddRange(own, other);
            await db.SaveChangesAsync();
            ownId = own.Id;
            otherId = other.Id;
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userA));

        var response = await client.GetAsync("/api/v2.0/notifications");
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = payload.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Contains(items, x => x.GetProperty("id").GetInt64() == ownId);
        Assert.DoesNotContain(items, x => x.GetProperty("id").GetInt64() == otherId);

        var foreignRead = await client.PutAsync($"/api/v2.0/notifications/{otherId}/read", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);

        var ownRead = await client.PutAsync($"/api/v2.0/notifications/{ownId}/read", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.NoContent, ownRead.StatusCode);
    }

    private static string CreateJwt(string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Student")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsASecretKeyForTestingPurposesOnly123456!"));
        var token = new JwtSecurityToken(
            issuer: "TmsTestIssuer",
            audience: "TmsTestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(20),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
