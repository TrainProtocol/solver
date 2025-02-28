using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Train.Solver.API;
using Train.Solver.API.Endpoints;
using Train.Solver.API.Extensions;
using Train.Solver.API.MIddlewares;
using Train.Solver.API.Swagger;
using Train.Solver.API.Validators;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Secret;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddAutoMapper(mc =>
{
    mc.AddProfile<MapperProfile>();
});

ValidatorOptions.Global.PropertyNameResolver = SnakeCasePropertyResolver.ResolvePropertyName;

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("solver", new() { Title = "Train Solver API", Version = "v1" });
    c.EnableAnnotations();
    c.SchemaFilter<SnakeCaseSchemaFilter>();
    c.CustomSchemaIds(i => i.FriendlyId());
    c.SupportNonNullableReferenceTypes();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services
    .AddTrainSolver(builder.Configuration)
    .AddAzureKeyVaultStorage(builder.Configuration);

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
