using System.Globalization;
using IntuneLight.Components;
using IntuneLight.Diagnostics;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Options;
using IntuneLight.Security;
using IntuneLight.Services;
using IntuneLight.Services.State;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Prometheus;
using Serilog;
using Vestfold.Extensions.Logging;
using Vestfold.Extensions.Metrics;

// Set Norwegian culture to ensure correct date and number formatting
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("nb-NO");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("nb-NO");

var builder = WebApplication.CreateBuilder(args);

// Vestfold.Extensions.Logging handles Serilog setup
builder.Logging.AddVestfoldLogging();

// Bind from appsettings + env vars
builder.Configuration.AddEnvironmentVariables();

// Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;

    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 1000;
    config.SnackbarConfiguration.ShowTransitionDuration = 1000;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 10;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Bind EntraId options from appsettings, user secrets and environment
builder.Services.Configure<EntraIdOptions>(builder.Configuration.GetSection("EntraId"));

// Bind HttpClients options (BaseAddress)
builder.Services.Configure<HttpClientsOptions>(builder.Configuration.GetRequiredSection("HttpClients"));

// Bind Pureservice options from appsettings (base address)
builder.Services.Configure<PureserviceOptions>(builder.Configuration.GetSection("Pureservice"));

// Register token service
builder.Services.AddSingleton<ITokenService, TokenService>();

// Named HttpClient for Microsoft Graph
builder.Services.AddHttpClient("Graph", (sp, client) =>
{
    var httpOptions = sp.GetRequiredService<IOptions<HttpClientsOptions>>().Value;
    if (string.IsNullOrWhiteSpace(httpOptions.Graph.BaseAddress))
        throw new InvalidOperationException("HttpClients:Graph:BaseAddress must be configured.");

    client.BaseAddress = new Uri(httpOptions.Graph.BaseAddress);
});

// Named HttpClient for Microsoft Defender
builder.Services.AddHttpClient("Defender", (sp, client) =>
{
    var httpOptions = sp.GetRequiredService<IOptions<HttpClientsOptions>>().Value;
    if (string.IsNullOrWhiteSpace(httpOptions.Defender.BaseAddress))
        throw new InvalidOperationException("HttpClients:Defender:BaseAddress must be configured.");

    client.BaseAddress = new Uri(httpOptions.Defender.BaseAddress);
});

// Named HttpClient for Pureservice
builder.Services.AddHttpClient("Pureservice", (sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<PureserviceOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseAddress);
});

// Register external api services
builder.Services.AddScoped<IIntuneService, IntuneService>();
builder.Services.AddScoped<IDefenderService, DefenderService>();
builder.Services.AddScoped<IEntraDirectoryService, EntraDirectoryService>();
builder.Services.AddScoped<IPureserviceService, PureserviceService>();

// Register ApiResponseGuard, centralized API error handling
builder.Services.AddScoped<IApiResponseGuard, ApiResponseGuard>();

// Register UiErrorHandler, centralized UI error handling
builder.Services.AddScoped<IUiErrorHandler, UiErrorHandler>();

// Register state service
builder.Services.AddScoped<DeviceLookupState>();

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure authentication schemes based on environment
if (builder.Environment.IsDevelopment())
{
    // In development, use a mock authentication handler that simulates an authenticated user with predefined claims.
    builder.Services.AddAuthentication("Mock")
        .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("Mock", null);
}
else
{
    // In production, use the EasyAuth authentication handler to integrate with Azure App Service Easy Auth.
    builder.Services.AddAuthentication("EasyAuth")
        .AddScheme<AuthenticationSchemeOptions, EasyAuthAuthenticationHandler>("EasyAuth", null);
}

// Define authorization policies based on roles from EntraId options
// [Authorize(Policy = Policy.Admin)]
builder.Services.AddAuthorization(options =>
{
    var entraOptions = builder.Configuration.GetSection("EntraId").Get<EntraIdOptions>();

    if (entraOptions is not null)
    {
        // Define policies that require specific roles from EntraId options
        options.AddPolicy(Policy.Admin, policy => policy.RequireRole(entraOptions.AppRoleAdmin));
        options.AddPolicy(Policy.User, policy => policy.RequireRole(entraOptions.AppRoleUser));
        options.AddPolicy(Policy.Metrics, policy => policy.RequireRole(entraOptions.AppRoleMetrics));

        // Grants access to users with either role
        options.AddPolicy(Policy.AnyUserRole, policy =>
            policy.RequireAssertion(ctx => 
                                    ctx.User.IsInRole(entraOptions.AppRoleAdmin) ||
                                    ctx.User.IsInRole(entraOptions.AppRoleUser)));
    }
    else
    {
        throw new InvalidOperationException("EntraId options must be configured for authorization policies.");
    }
});

// Register ActorContext and UserContext for per-request identity information
builder.Services.AddScoped<UserContext>();

// Add Vestfold Metrics for Prometheus instrumentation
builder.Services.AddVestfoldMetrics();

// Configure the service container to collect Prometheus metrics from all registered HttpClients
builder.Services.UseHttpClientMetrics();

// Keep Blazor circuit alive longer when app is in background
builder.Services.AddServerSideBlazor(options =>
{
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
});

// Keep SignalR connection alive longer before considering it lost
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseRouting();

// AuthN/Z must be after routing but before endpoints
app.UseAuthentication();
app.UseAuthorization();

// Antiforgery must be after routing (and after auth if present)
app.UseAntiforgery();

// HTTP request metrics
app.UseHttpMetrics();

// Exposes collected metrics in Prometheus text format.
// AllowAnonymous is required because the Prometheus scraper is a machine-to-machine process
// with no user identity or token. Access should be restricted at the network level in Azure instead.
app.MapGet("/metrics", async context =>
{
    context.Response.ContentType = "text/plain; version=0.0.4";
    await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(context.Response.Body);

}).RequireAuthorization(Policy.Metrics);

// Debug endpoint (open in dev, protected in prod)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/whoami", DebugEndpoints.WhoAmI);
}
else
{
    app.MapGet("/debug/whoami", DebugEndpoints.WhoAmI).RequireAuthorization();
}

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
