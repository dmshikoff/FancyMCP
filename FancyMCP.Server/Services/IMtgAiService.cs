using MtgChatBotPrototype.Models;

namespace FancyMCP;

public interface IMtgAiService
{
    Task<List<MtgCard>> UseDeckAiService(string message);
}
