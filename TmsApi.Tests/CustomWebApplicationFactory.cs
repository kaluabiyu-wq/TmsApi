using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using TmsApi.Infrastructure.Persistence;


namespace TmsApi_Tests;

public class CustomWebApplicatonFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context,config)=>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
           { 
        ["JWt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456!",
        ["Jwt:Secret"] = "ThisIsASecretKeyForTestingPurposeOnly123456!",
        ["Jwt:Issuer"] = "TmsTestIssuer",
        ["Jwt:Audience"] = "TmsTestAudience",

        // Tests send many requests from one address with no API key. Production keeps
        // the defaults (auth 5 per minute; anon 10, free 30, paid 200 tokens).
        ["RateLimiting:AuthPermitLimit"] = "1000",
        ["RateLimiting:AnonTokenLimit"] = "100000",
        ["RateLimiting:AnonTokensPerPeriod"] = "100000",
        ["RateLimiting:FreeTokenLimit"] = "100000",
        ["RateLimiting:FreeTokensPerPeriod"] = "100000",
        ["RateLimiting:PaidTokenLimit"] = "100000",
        ["RateLimiting:PaidTokensPerPeriod"] = "100000",

        // Tests use the in-memory provider: never migrate or seed, and never run the
        // admin bootstrap, even if a developer has these set in user-secrets.
        ["MigrateOnStartup"] = "false",
        ["Seed:RunOnStartup"] = "false",
        ["Bootstrap:AdminEmail"] = "",
        ["Bootstrap:AdminPassword"] = ""
          });
        });

        builder.ConfigureServices(services =>
        {
           services.RemoveAll<DbContextOptions<TmsDbContext>>();
           services.RemoveAll<DbContextOptions>();
           services.RemoveAll<TmsDbContext>();


           var inMemoryProvider = new ServiceCollection()
           .AddEntityFrameworkInMemoryDatabase()
           .BuildServiceProvider();

           services.AddDbContext<TmsDbContext>(options =>
           {
            options.UseInMemoryDatabase("TmsTestDb");
            options.UseInternalServiceProvider(inMemoryProvider);
               
           });
          

        });
    }
}