using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Train.Solver.API.Endpoints;
using Train.Solver.API.Extensions;
using Train.Solver.API.MIddlewares;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
using Train.Solver.Data.Npgsql.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromMinutes(3)));
}); 

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(httpContext.GetIpAddress(),
        partition => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 180,
            Window = TimeSpan.FromSeconds(60)
        }));
});


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("latest", new() { Title = "Train Solver Latest API", Version = "latest" });
    c.SwaggerDoc("v1", new() { Title = "Train Solver API v1", Version = "v1" });
    c.EnableAnnotations();
    c.CustomSchemaIds(i => i.FriendlyId());
    c.SupportNonNullableReferenceTypes();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddTrainSolver(builder.Configuration)
    .WithOpenTelemetryLogging("Solver API")
    .WithNpgsqlRepositories(opts => opts.MigrateDatabase = true);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.AllowAnyHeader();
            builder.AllowAnyMethod();
            builder.AllowAnyOrigin();
        });
});

var app = builder.Build();

app.MapGroup("/api")
    .MapGet("/health", () => Results.Ok())
    .WithTags("System")
    .Produces(StatusCodes.Status200OK);

app.MapGroup("/api")
   .MapV1Endpoints()
   .RequireRateLimiting("Fixed")
   .WithGroupName("latest")
   .WithTags("Endpoints");

app.MapGroup("/api/v1")
   .MapV1Endpoints()
   .RequireRateLimiting("Fixed")
   .WithGroupName("v1")
   .WithTags("Endpoints");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/latest/swagger.json", "Train Solver Latest API");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Train Solver API v1");
    c.DisplayRequestDuration();
});

app.UseOutputCache();
app.UseRateLimiter();
app.UseCors();
app.UseMiddleware<ErrorHandlerMiddleware>();

await app.RunAsync();
