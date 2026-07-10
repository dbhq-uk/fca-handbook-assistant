using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace FcaHandbookAssistant.Tests.Api;

/// <summary>
/// Boots the whole host (in Local mode) to prove wiring is valid and the health probe responds.
/// A parseable-but-unused connection string is supplied so startup succeeds without a database
/// (the data source connects lazily); the full ask flow is covered by the service unit tests.
/// </summary>
public class ApiSmokeTests : IClassFixture<ApiSmokeTests.Factory>
{
    readonly Factory _factory;

    public ApiSmokeTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Contains("ok", await response.Content.ReadAsStringAsync());
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Ai:Mode", "Local");
            builder.UseSetting(
                "ConnectionStrings:Postgres",
                "Host=localhost;Database=fca_handbook;Username=fca;Password=unused");
        }
    }
}
