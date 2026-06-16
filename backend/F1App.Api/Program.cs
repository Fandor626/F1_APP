using System.Text.Json.Serialization;

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
              .AllowAnyMethod());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (allowedOrigins.Length == 0)
{
    app.Logger.LogWarning(
        "AllowedOrigins is empty — CORS will block every cross-origin request. " +
        "Check that appsettings.Development.json (gitignored) exists and defines AllowedOrigins.");
}

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

app.Run();

public partial class Program;
