using System;
using MtgChatBotPrototype.Services;
using MtgChatBotPrototype.Models;
using Microsoft.Extensions.Logging;

namespace FancyMCP.Service.Services;

public class MtgAiService : IMtgAiService
{
    private readonly IDeckAiService _deckAiService;
    private readonly ILogger<MtgAiService> _logger;

    public MtgAiService(IDeckAiService deckAiService, ILogger<MtgAiService> logger)
    {
        _deckAiService = deckAiService;
        _logger = logger;
    }

    public async Task<List<MtgCard>> UseDeckAiService(string message)
    {
        _logger.LogInformation(">>> MtgAiService.UseDeckAiService called");
        _logger.LogInformation($">>> Calling DeckAiService.QueryOpenAiAsync with message: {message}");
        
        try
        {
            List<MtgCard> result = await _deckAiService.QueryOpenAiAsync(message);
            
            _logger.LogInformation($">>> DeckAiService returned {result?.Count ?? 0} cards");
            
            return result;
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            _logger.LogError(jsonEx, ">>> JSON deserialization error in DeckAiService");
            _logger.LogError($">>> JSON Path: {jsonEx.Path}");
            _logger.LogError($">>> This typically means Azure OpenAI returned data in an unexpected format for the MtgQuery model");
            _logger.LogError(">>> SOLUTION: Add [JsonConverter(typeof(StringOrStringArrayConverter))] to the Color property in MtgQuery");
            _logger.LogError(">>> See FancyMCP.Server/Converters/README.md for implementation details");
            
            // Re-throw with additional context
            throw new Exception(
                $"Failed to parse search results. The AI returned data in an unexpected format (Path: {jsonEx.Path}). " +
                $"To fix this, add the StringOrStringArrayConverter to the MtgQuery model in MtgChatBotPrototype. " +
                $"See FancyMCP.Server/Converters/README.md for details.", 
                jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> Unexpected error in DeckAiService");
            throw;
        }
    }
}
