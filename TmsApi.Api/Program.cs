using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using Asp.Versioning;
using TmsApi.Application.Interfaces;
using TmsApi.Api.Middlewares;
using TmsApi.Api.Filters;
using TmsApi.Infrastructure.Persistence.Services;
using TmsApi.Application.Enrollments.Commands;
using FluentValidation;
using MediatR;
using TmsApi.Application.Behaviors;
using TmsApi.Api.ExceptionHandlers;
using Microsoft.Extensions.Caching.Hybrid;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using TmsApi.Infrastructure.Transcripts;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Api.Hubs;
using TmsApi.Application.Notifications;
using TmsApi.Api.Notifications;
using Microsoft.AspNetCore.Antiforgery;
using TmsApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TmsApi.Api.Authorization;
using Microsoft.AspNetCore.Authorization;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>cfg.RegisterServicesFromAssemblies(typeof(EnrollStudentHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// builder.Services.AddStackExchangeRedisCache(options =>
// {
// options.Configuration = builder.Configuration.GetConnectionString("Redis");
// options.InstanceName = "tms:";
// });

builder.Services.AddHybridCache(options =>
{
options.DefaultEntryOptions = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromMinutes(10),
    LocalCacheExpiration = TimeSpan.FromMinutes(2)
    
};
});
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),

            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),

            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });
    options.AddConcurrencyLimiter("transcripts",opt =>
    {
        opt.PermitLimit = 5;
        opt.QueueLimit = 20;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        
    }

    );

    options.AddFixedWindowLimiter("AuthLimiter",opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        
    }
    
    );
    options.AddTokenBucketLimiter("search", opt =>
    {
       opt.TokenLimit = 10;
       opt.TokensPerPeriod = 5;
       opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
       opt.QueueLimit = 2;
        });
    

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };
});


var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy => 
    {
    policy.WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});



builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information)
.EnableSensitiveDataLogging()
);
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));



builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ITranscriptNotificationService,SignalRTranscriptNotificationService>();
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
new BoundedChannelOptions(100)
{
FullMode = BoundedChannelFullMode.Wait
}));
builder.Services.AddIdentityCore<TmsUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
   
    
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<TmsDbContext>();
// builder.Services.AddSignalR().AddAzureSignalR(
//     builder.Configuration.GetConnectionString("AzureSignalR"));

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<ICertficateService,CertificateService>();
builder.Services.AddScoped<ICachedCourseService,CachedCourseService>();
builder.Services.AddSingleton<ITranscriptStatusStore,InMemoryTranscriptStatusStore>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<IAuthorizationHandler, CourseInstructoreHandler>();


builder.Services.AddAuthentication(options =>
{
   options.DefaultAuthenticateScheme = 
   JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = 
    JwtBearerDefaults.AuthenticationScheme;


}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]!)
        )
    };
});
builder.Services.AddAuthorizationBuilder()
.AddPolicy("CanEditCourse", policy =>
 policy.Requirements.Add(new CourseInstructorRequirement ()));


builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
    description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description =>
    description.GroupName == "v2";
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var service = new CryptoDemoService();
string hash1 = service.HashUserPassword("Password123!");
string hash2 = service.HashUserPassword("Password123!");

Console.WriteLine($"Hash 1: {hash1}");
Console.WriteLine($"Hash 2: {hash2}");

bool match1 = service.VerifyUserPassword("Password!23!",hash1);
bool match2 = service.VerifyUserPassword("Password!23!",hash2);



var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
}

app.UseHttpsRedirection();

app.UseCors("TmsClient");

app.Use(async (context, next) =>
{
    // Skip strict CSP for Scalar's API docs UI (needs inline scripts to render)
    if (!context.Request.Path.StartsWithSegments("/scalar"))
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';"
        );
    }
    await next();
});
 
app.Use(async (context,next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies
           .ContainsKey("tms_auth")
    )
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

        var tokens = antiforgery.GetAndStoreTokens(context);

        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
        
           new CookieOptions
           {
               HttpOnly = false,
               Secure = !builder.Environment.IsDevelopment(),
               SameSite = SameSiteMode.Strict
           });

    }
    await next(context);
});
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<V1DeprecationMiddleware>();


app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");
app.MapControllers();

app.Run();
