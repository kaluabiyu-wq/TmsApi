using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
.AddAuthentication("Training")
.AddScheme<AuthenticationSchemeOptions,TrainingAuthHandler>("Training",null);

builder.Services.AddOptions<PaymentOptions>()
.BindConfiguration("Payments")
.ValidateDataAnnotations()
.ValidateOnStart();


builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Host.UseDefaultServiceProvider(options =>
{
options.ValidateScopes = true;
options.ValidateOnBuild = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// app.UseHttpsRedirection();
// app.MapGet("/api/assessments/results",()=>Results.Ok(new
// {
//     courseCode = "CS-101",
//     studentId = "S-001",
//     letterGrade = "A"
// })).RequireAuthorization();

app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
worker.ProcessBatch();
return Results.Ok("processed");
});
// app.MapControllers();

app.Run();
