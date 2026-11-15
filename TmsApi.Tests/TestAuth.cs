using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TmsApi_Tests;

/// <summary>
/// One shared way to sign a test client in with a given role, so new test classes
/// do not each copy their own GenerateTestJwt. The key, issuer and audience match
/// the values set in CustomWebApplicatonFactory.
/// </summary>
internal static class TestAuth
{
    public static void SignInAs(this HttpClient client, string role, string userId = "test-user-id") =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role, userId));

    public static void SignOut(this HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = null;

    private static string CreateToken(string role, string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ThisIsASecretKeyForTestingPurposesOnly123456!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TmsTestIssuer",
            audience: "TmsTestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
