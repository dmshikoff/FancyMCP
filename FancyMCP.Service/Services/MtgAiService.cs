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
        
        var result = await _deckAiService.QueryOpenAiAsync(message);
        
        _logger.LogInformation($">>> DeckAiService returned {result?.Count ?? 0} cards");
        
        return result;
    }
}
