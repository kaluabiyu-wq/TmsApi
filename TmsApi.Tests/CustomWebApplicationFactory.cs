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
        ["Jwt:Audience"] = "TmsTestAudience"
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