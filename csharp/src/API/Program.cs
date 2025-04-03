using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Train.Solver.API;
using Train.Solver.API.Endpoints;
using Train.Solver.API.Extensions;
using Train.Solver.API.MIddlewares;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
using Train.Solver.Repositories.Npgsql.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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

builder.Services.AddAutoMapper(mc =>
{
    mc.AddProfile<MapperProfile>();
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("solver", new() { Title = "Train Solver API", Version = "v1" });
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
   .MapEndpoints()
   .RequireRateLimiting("Fixed")
   .WithGroupName("solver")
   .WithTags("Endpoints");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/solver/swagger.json", "solver");
    c.DisplayRequestDuration();
});

app.UseRateLimiter();
app.UseCors();
app.UseMiddleware<ErrorHandlerMiddleware>();

await app.RunAsync();
