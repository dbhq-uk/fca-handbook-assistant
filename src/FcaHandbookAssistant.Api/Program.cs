using FcaHandbookAssistant.Api;
using FcaHandbookAssistant.Api.Components;
using FcaHandbookAssistant.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHandbookAssistant(builder.Configuration);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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
