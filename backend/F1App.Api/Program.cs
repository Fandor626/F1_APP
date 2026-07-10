using System.Net;
using System.Text.Json.Serialization;
using F1App.Api.Clients;
using F1App.Api.Hubs;
using F1App.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("F1App.Api.Tests")]

var builder = WebApplication.CreateBuilder(args);

const string FrontendDevCorsPolicy = "FrontendDev";

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // required for SignalR WebSocket upgrade
});

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

// Trailing slash is required: HttpClient resolves a relative request URI
// ("current.json") against BaseAddress by replacing the last path segment
// unless BaseAddress itself ends in "/".
var ergastBaseUrl = builder.Configuration["ErgastBaseUrl"]!.TrimEnd('/') + "/";
builder.Services.AddHttpClient<IErgastClient, ErgastClient>(client =>
{
    client.BaseAddress = new Uri(ergastBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var openF1BaseUrl = builder.Configuration["OpenF1BaseUrl"]!.TrimEnd('/') + "/";
builder.Services.AddHttpClient<IOpenF1Client, OpenF1Client>(client =>
{
    client.BaseAddress = new Uri(openF1BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<RaceScheduleService>();
builder.Services.AddScoped<StandingsService>();
builder.Services.AddScoped<SeasonWrappedService>();
builder.Services.AddScoped<WinProbabilityService>();
builder.Services.AddScoped<PitWindowService>();
builder.Services.AddScoped<CircuitProfileService>();
builder.Services.AddScoped<DriverProfileService>();
builder.Services.AddScoped<HeadToHeadService>();
builder.Services.AddScoped<IFeedReaderClient, FeedReaderClient>();
builder.Services.AddScoped<NewsFeedService>();
builder.Services.AddHostedService<RaceDataOrchestrator>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (allowedOrigins.Length == 0)
{
    app.Logger.LogWarning(
        "AllowedOrigins is empty — CORS will block every cross-origin request. " +
        "Check that appsettings.Development.json (gitignored) exists and defines AllowedOrigins.");
}

// Global exception handling: controllers/services throw, this converts to
// ProblemDetails (RFC 7807). Upstream data-source failures (Ergast/OpenF1
// unreachable or timing out) map to 502 so the frontend can distinguish
// "our bug" from "their API is down" and show the right error copy.
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var isUpstreamFailure = exception is HttpRequestException or TaskCanceledException or InvalidOperationException;

    if (isUpstreamFailure)
    {
        app.Logger.LogWarning(exception, "Upstream data source unavailable");
        context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
    }
    else
    {
        app.Logger.LogError(exception, "Unhandled exception");
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    }

    await Results.Problem(
        title: isUpstreamFailure ? "Upstream data source unavailable" : "An unexpected error occurred",
        statusCode: context.Response.StatusCode
    ).ExecuteAsync(context);
}));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS must run before HTTPS redirection — a redirect response carries no CORS
// headers, so a cross-origin request hitting the HTTP endpoint first would be
// blocked by the browser instead of transparently following the redirect.
app.UseCors(FrontendDevCorsPolicy);

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<RaceHub>("/hubs/race");

app.Run();

public partial class Program;
