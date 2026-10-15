using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;

namespace RaceDay.API.Test
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    DbContextOptions<RaceDayDbContext>>();

                services.RemoveAll<RaceDayDbContext>();

                services.AddDbContext<RaceDayDbContext>(
    options => options.UseInMemoryDatabase(
        "RaceDayIntegrationTestDB"));
            });
        }
    }
}