var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Endpoints, ProblemDetails, logging and /health are added in F1-03.

await app.RunAsync();

/// <summary>Entry point; public so integration tests can use WebApplicationFactory.</summary>
public partial class Program;
