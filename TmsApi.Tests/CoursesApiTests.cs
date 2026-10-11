using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TmsApi_Tests;

public class CoursesApiTests : IClassFixture<CustomWebApplicatonFactory>
{
    private readonly HttpClient _client;

    public CoursesApiTests(CustomWebApplicatonFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SignInAsInstructor() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestJwt());

    [Fact]
    public async Task GetCourses_ReturnsOkAndPagedJson()
    {
        // Every endpoint now requires a signed-in user (fallback policy).
        SignInAsInstructor();

        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedCoursesJson>();
        Assert.NotNull(page?.Items);
    }

    [Fact]
    public async Task GetCourses_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v2.0/courses?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_InvalidCode_ReturnsValidationError()
    {
        SignInAsInstructor();

        var response = await _client.PostAsJsonAsync("/api/v2.0/courses", new
        {
            code = "",
            title = "Intro to TMS Security",
            maxCapacity = 30
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_IgnoresRequestedRole_AndAssignsStudent()
    {
        const string email = "role-escalation-test@example.com";
        const string password = "Str0ng!Passw0rd#1";

        // An attacker tries to pick their own role.
        var register = await _client.PostAsJsonAsync("/api/v1.0/auth/register", new
        {
            email,
            password,
            firstName = "Eve",
            lastName = "Tester",
            role = "Admin"
        });
        register.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1.0/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginJson>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        var roles = jwt.Claims
            .Where(c => c.Type == "role" || c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        Assert.Equal(new[] { "Student" }, roles);
    }

    private static string GenerateTestJwt()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-instructor-id"),
            new Claim(ClaimTypes.Role, "Instructor"),
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

    private sealed class PagedCoursesJson
    {
        public List<CourseRowJson> Items { get; set; } = default!;
        public int TotalCount { get; set; }
    }

    private sealed class CourseRowJson
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public int MaxCapacity { get; set; }
        public int EnrollmentCount { get; set; }
    }
}