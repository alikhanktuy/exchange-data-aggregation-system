using SimulatedServer;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();
app.UseWebSockets();
MockExchangeServer.MapEndpoints(app);
app.MapGet("/", () => Results.Ok("Mock exchange server is running."));
app.Run();

/// <summary>Program marker for integration tests.</summary>
public partial class Program;
