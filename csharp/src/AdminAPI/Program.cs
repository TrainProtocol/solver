using System.Text.Json.Serialization;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
using Train.Solver.Data.Npgsql.Extensions;
using Train.Solver.Common.Extensions;
using Train.Solver.AdminAPI.Endpoints;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastrucutre.Secret.Treasury.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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
    c.SwaggerDoc("v1", new() { Title = "Train Solver Admin API", Version = "v1" });
    c.EnableAnnotations();
    c.CustomSchemaIds(i => i.FriendlyId());
    c.SupportNonNullableReferenceTypes();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddTrainSolver(builder.Configuration)
    .WithTreasury()
    .WithOpenTelemetryLogging("Solver Admin API")
    .WithNpgsqlRepositories();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGroup("/api")
    .MapGet("/health", () => Results.Ok())
    .WithTags("System")
    .Produces(StatusCodes.Status200OK);

app.MapGroup("/api/v1")
   .MapAdminEndpoints()
   .RequireRateLimiting("Fixed")
   .WithGroupName("v1")
   .WithTags("Endpoints");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Train Solver Admin API v1");
    c.DisplayRequestDuration();
});

await app.RunAsync();
