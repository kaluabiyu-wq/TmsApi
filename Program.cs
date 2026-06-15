using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

builder.Services.AddControllers();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware pipeline (order matters)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Protected assessment endpoint
app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId  = "S-001",
        letterGrade = "A"
    });
}).RequireAuthorization();


app.MapPost("/api/enrollments", async (IEnrollmentService svc) =>
{
    var record = await svc.EnrollAsync("S-001", "CS-101");
    return Results.Ok(record);
});

app.MapGet("/api/enrollments/{id}", async (string id, IEnrollmentService svc) =>
{
    var record = await svc.GetByIdAsync(id);
    return record is null ? Results.NotFound() : Results.Ok(record);
});

app.MapGet("/api/enrollments", async (IEnrollmentService svc) =>
{
    var all = await svc.GetAllAsync();
    return Results.Ok(all);
});

app.Run();