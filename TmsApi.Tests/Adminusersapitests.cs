using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TmsApi_Tests;

public class AdminUsersApiTests : IClassFixture<CustomWebApplicatonFactory>
{
    private const string RoleUrl = "/api/v2.0/admin/users/role";
    private readonly HttpClient _client;

    public AdminUsersApiTests(CustomWebApplicatonFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SetRole_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync(RoleUrl,
            new { email = "someone@example.com", role = "Instructor" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Instructor")]
    public async Task SetRole_AsNonAdmin_ReturnsForbidden(string callerRole)
    {
        Authenticate(callerRole);

        var response = await _client.PutAsJsonAsync(RoleUrl,
            new { email = "someone@example.com", role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetRole_UnknownRole_ReturnsBadRequest()
    {
        Authenticate("Admin");

        var response = await _client.PutAsJsonAsync(RoleUrl,
            new { email = "someone@example.com", role = "SuperUser" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetRole_AsAdmin_PromotesStudentToInstructor()
    {
        const string email = "promote-me@example.com";
        const string password = "Str0ng!Passw0rd#1";

        var register = await _client.PostAsJsonAsync("/api/v1.0/auth/register", new
        {
            email,
            password,
            firstName = "Pat",
            lastName = "Student"
        });
        register.EnsureSuccessStatusCode();

        Authenticate("Admin");
        var promote = await _client.PutAsJsonAsync(RoleUrl, new { email, role = "Instructor" });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        // The new role shows up the next time the user logs in.
        _client.DefaultRequestHeaders.Authorization = null;
        var login = await _client.PostAsJsonAsync("/api/v1.0/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginJson>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        var roles = jwt.Claims
            .Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Assert.Equal(new[] { "Instructor" }, roles);
    }

    private void Authenticate(string role) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestJwt(role));

    private static string GenerateTestJwt(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-caller-id"),
            new Claim(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ThisIsASecretKeyForTestingPurposesOnly123456!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TmsTestIssuer",
            audience: "TmsTestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class LoginJson
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}