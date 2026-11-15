using System.Net;
using System.Net.Http.Json;

namespace TmsApi_Tests;

public class ClassroomsApiTests : IClassFixture<CustomWebApplicatonFactory>
{
    private const string Url = "/api/v2.0/classrooms";
    private readonly HttpClient _client;

    public ClassroomsApiTests(CustomWebApplicatonFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueName(string prefix = "Room") =>
        $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(Url, new { name = UniqueName(), capacity = 10 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("Instructor")]
    public async Task Create_AsNonAdmin_ReturnsForbidden(string role)
    {
        _client.SignInAs(role);

        var response = await _client.PostAsJsonAsync(Url, new { name = UniqueName(), capacity = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreatedWithLocationAndBody()
    {
        _client.SignInAs("Admin");
        var name = UniqueName("Lab");

        var response = await _client.PostAsJsonAsync(Url,
            new { name, location = "Building A", capacity = 25 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<ClassroomJson>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(name, created.Name);
        Assert.Equal(25, created.Capacity);
        Assert.True(created.IsActive);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Valid name", 0)]
    [InlineData("Valid name", 1001)]
    public async Task Create_InvalidInput_ReturnsValidationProblem(string name, int capacity)
    {
        _client.SignInAs("Admin");

        var response = await _client.PostAsJsonAsync(Url, new { name, capacity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_DuplicateNameIgnoringCase_ReturnsConflict()
    {
        _client.SignInAs("Admin");
        var name = UniqueName("Dup");

        var first = await _client.PostAsJsonAsync(Url, new { name, capacity = 10 });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync(Url, new { name = name.ToUpperInvariant(), capacity = 10 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task List_AsInstructor_ReturnsPagedJsonContainingCreatedRoom()
    {
        _client.SignInAs("Admin");
        var name = UniqueName("Listed");
        var create = await _client.PostAsJsonAsync(Url, new { name, capacity = 12 });
        create.EnsureSuccessStatusCode();

        _client.SignInAs("Instructor");
        var response = await _client.GetAsync($"{Url}?page=1&pageSize=50");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedClassroomsJson>();

        Assert.NotNull(page);
        Assert.True(page!.TotalCount >= 1);
        Assert.Equal(1, page.Page);
        Assert.Contains(page.Items, c => c.Name == name);
    }

    [Fact]
    public async Task List_AsStudent_ReturnsForbidden()
    {
        _client.SignInAs("Student");

        var response = await _client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        _client.SignInAs("Admin");

        var response = await _client.GetAsync($"{Url}/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsAdmin_CanDeactivateRoom_AndFilterByIsActive()
    {
        _client.SignInAs("Admin");
        var name = UniqueName("Retire");
        var create = await _client.PostAsJsonAsync(Url, new { name, location = "B2", capacity = 8 });
        var created = await create.Content.ReadFromJsonAsync<ClassroomJson>();

        var update = await _client.PutAsJsonAsync($"{Url}/{created!.Id}",
            new { name, location = "B2", capacity = 8, isActive = false });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ClassroomJson>();
        Assert.False(updated!.IsActive);

        var activeOnly = await _client.GetFromJsonAsync<PagedClassroomsJson>($"{Url}?isActive=true&pageSize=50");
        Assert.DoesNotContain(activeOnly!.Items, c => c.Id == created.Id);

        var inactiveOnly = await _client.GetFromJsonAsync<PagedClassroomsJson>($"{Url}?isActive=false&pageSize=50");
        Assert.Contains(inactiveOnly!.Items, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        _client.SignInAs("Admin");

        var response = await _client.PutAsJsonAsync($"{Url}/999999",
            new { name = UniqueName(), capacity = 5, isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class ClassroomJson
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class PagedClassroomsJson
    {
        public List<ClassroomJson> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
