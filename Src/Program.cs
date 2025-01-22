using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProximoTurno.ManualDoJogo.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddHttpClient();
builder.Services.AddScoped<GeminiApi>();
builder.Services.AddSingleton<DatabaseApi>(serviceProvider => {
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<DatabaseApi>>();
    return DatabaseApi.Init(logger, configuration);
});

builder.Logging.AddConsole();
var app = builder.Build();
app.Run();
