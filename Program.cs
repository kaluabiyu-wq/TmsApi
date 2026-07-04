using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<TmSDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information)
.EnableSensitiveDataLogging()
);

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<IStudentService, StudentService>();
builder.Services.AddSingleton<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseServices, CourseServices>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();



builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddControllers();
// .AddJsonOptions(options =>
// {
//     options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
// });


builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}





// Middleware pipeline (order matters)


app.UseMiddleware<RequestLoggingMiddleware>();
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// Protected assessment endpoint
app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId = "S-001",
        letterGrade = "A"
    });
}).RequireAuthorization();

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmSDbContext>();
    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() {RegistrationNumber = "TMS-2026-0001",Name ="Alice Smith",
            GPA = 3.8m, IsActive = true},
             new() {RegistrationNumber = "TMS-2026-0002",Name ="Bob Jones",
            GPA = 2.9m, IsActive = true},
             new() {RegistrationNumber = "TMS-2026-0003",Name ="Charlie Brown",
            GPA = 3.4m, IsActive = false},
             new() {RegistrationNumber = "TMS-2026-0004",Name ="Diana Prince",
            GPA = 3.9m, IsActive = true},
             new() {RegistrationNumber = "TMS-2026-0005",Name ="Evan Wright",
            GPA = 2.5m, IsActive = true}
        };

        context.Students.AddRange(students);
        //  context.Entry(students).Property("Last Updated").CurrentValue = DateTime.UtcNow;


        var courses = new List<Course>
        {
            new() {Code = "CS-101",Title="Introduction to Computer Science",
            MaxCapacity =30},
            new() {Code = "CS-201",Title="Data Structures and Algorithms",
            MaxCapacity =25},
             new() {Code = "MAT-101",Title="Calculus I",MaxCapacity =40}
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();
        var enrollments = new List<Enrollment>
        {
            new(){StudentId = students[0].ID,CourseId=courses[0].Id,
            Grade = 4.0m},
            new(){StudentId = students[0].ID,CourseId=courses[1].Id,
            Grade = 3.6m},
            new(){StudentId = students[1].ID,CourseId=courses[0].Id,
            Grade = 2.8m},
            new(){StudentId = students[3].ID,CourseId=courses[1].Id,
            Grade = 3.9m},
        };
        context.Enrollments.AddRange(enrollments);

        context.SaveChanges();
    }


}

// app.MapPut("/api/students/{id}", async (int id, Student students1, TmSDbContext context) =>
// {
//     var student = await context.Students.FindAsync(id);

//     try
//     {
//         await context.SaveChangesAsync();
//     }
//     catch (DbUpdateConcurrencyException)
//     {
//         return Results.Conflict("The record was modified by another user. ");
//     }

//     return Results.Ok(student);
// });

app.Run();

