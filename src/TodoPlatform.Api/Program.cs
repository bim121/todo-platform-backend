using Serilog;
using TodoPlatform.Api.Auth;
using TodoPlatform.Api.Exceptions;
using TodoPlatform.Api.Extensions;
using TodoPlatform.Api.Swagger;
using TodoPlatform.Api.Versioning;
using TodoPlatform.Application;
using TodoPlatform.Infrastructure;
using TodoPlatform.Infrastructure.Behaviors;
using TodoPlatform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(cfg => cfg.AddOpenBehavior(typeof(TransactionBehavior<,>)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiSwagger(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

await app.MigrateDevDatabaseAsync();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseApiSwaggerUi();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseCurrentUserEnrichment();
app.UseMiddleware<AuthorizationProblemDetailsMiddleware>();
app.UseAuthorization();
app.UseApiVersioning();
app.UseDeprecationHeaders();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program;
