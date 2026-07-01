var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/ping", () => "Healthy");
app.Run();
