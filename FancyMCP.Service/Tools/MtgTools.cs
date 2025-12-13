using System.ComponentModel;
using System.Text.Json;
using FancyMCP.Service.Services;
using ModelContextProtocol.Server;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace FancyMCP.Service.Tools;

[McpServerToolType]
public static class MtgTools
{
    [McpServerTool, Description("Search for Magic: The Gathering cards by description and provide a natural language summary")]
    public static async Task<string> SearchMtgCards(
        IMtgAiService mtgAiService, 
        AzureOpenAIClient azureClient,
        IConfiguration configuration,
        ILogger<Program> logger,
        string query)
    {
        // Write to a log file for easy verification
        var logPath = Path.Combine(AppContext.BaseDirectory, "mcp-tool-calls.log");
        await File.AppendAllTextAsync(logPath, 
            $"\n=== MCP Tool Called at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\nQuery: {query}\n");
        
        try
        {
            logger.LogInformation("=== MCP Server Tool Called ===");
            logger.LogInformation($"Query received: {query}");
            logger.LogInformation($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            var cards = await mtgAiService.UseDeckAiService(query);
            
            logger.LogInformation($"Cards found: {cards?.Count ?? 0}");
            await File.AppendAllTextAsync(logPath, $"Cards found: {cards?.Count ?? 0}\n");
            
            if (cards == null)
            {
                logger.LogError("Service returned null");
                await File.AppendAllTextAsync(logPath, "ERROR: Service returned null\n");
                return "I encountered an error while searching for cards.";
            }
            
            if (cards.Count == 0)
            {
                logger.LogInformation("No cards matched the query");
                await File.AppendAllTextAsync(logPath, "No cards matched\n");
                return "I couldn't find any cards matching that description. Try rephrasing your query or being more specific about what you're looking for.";
            }
            
            logger.LogInformation($"Calling Azure OpenAI to summarize {cards.Count} cards");
            await File.AppendAllTextAsync(logPath, $"Calling Azure OpenAI to summarize...\n");
            
            // Get the chat client
            var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? throw new Exception("DeploymentName not configured");
            var chatClient = azureClient.GetChatClient(deploymentName);
            
            // Serialize cards for the AI to analyze
            var cardsJson = JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true });
            
            // Ask the AI to provide a natural language summary
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful Magic: The Gathering assistant. When given card data in JSON format, provide a natural, conversational summary of the cards. Highlight the most interesting or relevant cards for the user's query. Be friendly and enthusiastic about the cards you're describing."),
                new UserChatMessage($"The user asked: '{query}'\n\nI found these cards:\n{cardsJson}\n\nPlease provide a natural language summary of these cards for the user.")
            };
            
            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 1
            };
            
            var response = await chatClient.CompleteChatAsync(messages, chatOptions);
            
            logger.LogInformation("Azure OpenAI response received successfully");
            logger.LogInformation("=== MCP Server Tool Complete ===");
            await File.AppendAllTextAsync(logPath, "SUCCESS: Response generated\n");
            
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SearchMtgCards tool");
            await File.AppendAllTextAsync(logPath, $"ERROR: {ex.Message}\n");
            return $"I encountered an error: {ex.Message}";
        }
    }
}
