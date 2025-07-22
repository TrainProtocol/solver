using System.Text.Json.Serialization;
using Train.Solver.AdminAPI.Endpoints;
using Train.Solver.Common.Extensions;
using Train.Solver.Common.Serialization;
using Train.Solver.Common.Swagger;
using Train.Solver.Data.Npgsql.Extensions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Infrastructure.Logging.OpenTelemetry;
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
    options.SerializerOptions.Converters.Add(new BigIntegerConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new BigIntegerConverter());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Train Solver Admin API" });
    c.EnableAnnotations();
    c.CustomSchemaIds(i => i.FriendlyId());
    c.SupportNonNullableReferenceTypes();
    c.SchemaFilter<BigIntegerSchemaFilter>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddTrainSolver(builder.Configuration)
    .WithCoreServices()
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
   .MapNetworkEndpoints()
   .RequireRateLimiting("Fixed")
   .WithTags("Network");

app.MapGroup("/api")
   .MapWalletEndpoints()
   .RequireRateLimiting("Fixed")
   .WithTags("Wallet");

app.MapGroup("/api")
   .MapFeeEndpoints()
   .RequireRateLimiting("Fixed")
   .WithTags("Fee");

app.MapGroup("/api")
   .MapRouteEndpoints()
   .RequireRateLimiting("Fixed")
   .WithTags("Route");

app.MapGroup("/api")
   .MapRateProviderEndpoints()
   .RequireRateLimiting("Fixed")
   .WithTags("Rate Provider");


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Train Solver Admin API");
    c.DisplayRequestDuration();
});

await app.RunAsync();
