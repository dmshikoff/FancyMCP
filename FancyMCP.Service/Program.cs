using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Azure;
using Azure.AI.OpenAI;
using MtgChatBotPrototype.Services;
using Microsoft.Extensions.Configuration;
using FancyMCP.Service.Services;
using FancyMCP;

// Set environment to Development for better debugging
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Get configuration values
string? azureOpenAiUrl = builder.Configuration["AzureOpenAI:Endpoint"];
string? azureOpenAiKey = builder.Configuration["AzureOpenAI:ApiKey"];

if (string.IsNullOrEmpty(azureOpenAiKey) || string.IsNullOrEmpty(azureOpenAiUrl))
{
    throw new Exception("invalid OpenAI Key or URI");
}

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register Azure OpenAI client
builder.Services.AddSingleton(new AzureOpenAIClient(
    new Uri(azureOpenAiUrl),
    new AzureKeyCredential(azureOpenAiKey)
));

// Register MTG services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMtgApiClient>(sp =>
{
    IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    HttpClient httpClient = httpClientFactory.CreateClient();
    return new MtgApiClient(httpClient);
});
builder.Services.AddSingleton<IDeckAiService>(sp =>
{
    AzureOpenAIClient azureClient = sp.GetRequiredService<AzureOpenAIClient>();
    IMtgApiClient mtgClient = sp.GetRequiredService<IMtgApiClient>();
    IConfiguration config = builder.Configuration;
    return new DeckAiService(azureClient, mtgClient, config);
});
builder.Services.AddSingleton<IMtgAiService, MtgAiService>();

await builder.Build().RunAsync();