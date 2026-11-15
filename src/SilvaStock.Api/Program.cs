using SilvaStock.Api.Cors;
using SilvaStock.Api.ErrorHandling;
using SilvaStock.Api.Health;
using SilvaStock.Api.Logging;
using SilvaStock.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddApiJsonLogging();

builder.Services.AddApiProblemDetails();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiHealthChecks();

var app = builder.Build();

app.UseApiErrorHandling();
app.UseCors();

app.MapApiHealthChecks();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;
