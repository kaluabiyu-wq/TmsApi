using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TmsApi_Tests;

public class AccountApiTests : IClassFixture<CustomWebApplicatonFactory>
{
    private const string ChangeUrl = "/api/v2.0/account/change-password";
    private readonly HttpClient _client;

    public AccountApiTests(CustomWebApplicatonFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(ChangeUrl,
            new { currentPassword = "x", newPassword = "y" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReplacesPassword()
    {
        const string email = "change-password-test@example.com";
        const string oldPassword = "Old!Passw0rd#12345";
        const string newPassword = "New!Passw0rd#67890";

        var register = await _client.PostAsJsonAsync("/api/v1.0/auth/register", new
        {
            email,
            password = oldPassword,
            firstName = "Pat",
            lastName = "Changer"
        });
        register.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1.0/auth/login",
            new { email, password = oldPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginJson>())!.AccessToken;

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        var change = await _client.PostAsJsonAsync(ChangeUrl,
            new { currentPassword = oldPassword, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;

        var oldLogin = await _client.PostAsJsonAsync("/api/v1.0/auth/login",
            new { email, password = oldPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync("/api/v1.0/auth/login",
            new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    private sealed class LoginJson
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}