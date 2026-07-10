using Azure.Monitor.OpenTelemetry.AspNetCore;
using FcaHandbookAssistant.Api;
using FcaHandbookAssistant.Api.Components;
using FcaHandbookAssistant.Api.Endpoints;
using FcaHandbookAssistant.Core.Observability;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHandbookAssistant(builder.Configuration);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Export traces, metrics (including the custom regulated metrics), and logs to Application
// Insights when configured; otherwise the app runs with no telemetry export (local/dev).
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics.AddMeter(HandbookMetrics.MeterName))
        .UseAzureMonitor(options => options.ConnectionString = appInsightsConnectionString);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapAskEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

// Exposed so the test host (WebApplicationFactory) can reference the entry point.
public partial class Program;
