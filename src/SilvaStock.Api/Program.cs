using SilvaStock.Api.Auth;
using SilvaStock.Api.Cors;
using SilvaStock.Api.ErrorHandling;
using SilvaStock.Api.Health;
using SilvaStock.Api.Logging;
using SilvaStock.Infrastructure;
using SilvaStock.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddApiJsonLogging();

builder.Services.AddApiProblemDetails();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAdminAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApiHealthChecks();

var app = builder.Build();

app.UseApiErrorHandling();
app.UseCors();

// Handles the Google callback (/api/auth/callback/google) and reads the session cookie.
app.UseAuthentication();
app.UseAuthorization();

app.MapApiHealthChecks();
app.MapAuthEndpoints();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;
