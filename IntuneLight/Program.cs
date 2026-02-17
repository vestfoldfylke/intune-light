using IntuneLight.Components;
using IntuneLight.Infrastructure;
using IntuneLight.Models.Options;
using IntuneLight.Services;
using IntuneLight.Services.State;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using Vestfold.Extensions.Logging;

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

// Bind EntraId options from appsettings and environment
builder.Services.Configure<EntraIdOptions>(options =>
{
    var section = builder.Configuration.GetSection("EntraId");
    section.Bind(options);

    var clientSecret = Environment.GetEnvironmentVariable("INTUNE_LIGHT_CS");
    if (string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException("Environment variable INTUNE_LIGHT_CS is not set.");
    }

    options.ClientSecret = clientSecret;
});

// Bind HttpClients options (BaseAddress)
builder.Services.Configure<HttpClientsOptions>(builder.Configuration.GetRequiredSection("HttpClients"));

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

// Bind Pureservice options from appsettings (base address)
builder.Services.Configure<PureserviceOptions>(builder.Configuration.GetSection("Pureservice"));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseSerilogRequestLogging();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
