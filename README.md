# FancyMCP - Magic: The Gathering Card Search with Model Context Protocol

A demonstration of the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) implemented in C# using the official SDK, showcasing how to build MCP servers and clients that leverage Azure OpenAI for natural language interactions.

## Project Overview

This solution demonstrates a complete MCP implementation consisting of:
- **FancyMCP.Server** - An MCP server that exposes MTG card search functionality
- **FancyMCP.Console** - An MCP client that provides an interactive console interface
- **MtgChatBotPrototype** - The underlying card search engine powered by Azure OpenAI

## Quick Start

If you encounter JSON deserialization errors with Azure OpenAI function calling, see [FIXING_JSON_ERROR.md](FIXING_JSON_ERROR.md) for the solution.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     FancyMCP.Console (Client)                   │
│  • Starts MCP server as child process                           │
│  • Connects via stdio transport                                 │
│  • Sends user queries to MCP server                             │
│  • Displays natural language responses                          │
└────────────────────────┬────────────────────────────────────────┘
                         │ MCP Protocol (stdio)
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                    FancyMCP.Server (MCP Server)                 │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │ 
│  │              MCP Tool: search_mtg_cards                 │    │
│  │  • Receives user query                                  │    │
│  │  • Calls MtgAiService                                   │    │
│  │  • Gets structured card data                            │    │
│  │  • Asks Azure OpenAI to summarize in natural language   │    │
│  │  • Returns conversational response                      │    │
│  └──────────────────────┬──────────────────────────────────┘    │
│                         │                                       │
│  ┌──────────────────────▼──────────────────────────────────┐    │
│  │                   MtgAiService                          │    │
│  │  • Wrapper for DeckAiService                            │    │
│  │  • Dependency injection bridge                          │    │
│  └──────────────────────┬──────────────────────────────────┘    │
└─────────────────────────┼───────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│          MtgChatBotPrototype.DeckAiService (Core Logic)         │
│  • Uses Azure OpenAI with function calling                      │
│  • Searches Scryfall API for MTG cards                          │
│  • Returns structured List<MtgCard> data                        │
└─────────────────────────────────────────────────────────────────┘
```

## FancyMCP.Server - The MCP Server

### Design Philosophy

The MCP server is built following the official [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) patterns. It exposes card search functionality as an MCP tool while delegating the actual search logic to the existing MtgChatBotPrototype project.

### Server Configuration

The server is configured in `Program.cs` using the Host builder pattern:

```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()                    // Register MCP server
    .WithStdioServerTransport()        // Use stdio for communication
    .WithToolsFromAssembly();          // Auto-discover tools

// Register dependencies
builder.Services.AddSingleton<AzureOpenAIClient>(...);
builder.Services.AddSingleton<IMtgApiClient>(...);
builder.Services.AddSingleton<IDeckAiService>(...);
builder.Services.AddSingleton<IMtgAiService, MtgAiService>();
```

**Key Points:**
- **Stdio Transport**: The server communicates via standard input/output, allowing clients to spawn it as a child process
- **Automatic Tool Discovery**: The `WithToolsFromAssembly()` method scans for classes marked with `[McpServerToolType]` and registers their methods as tools
- **Dependency Injection**: All services are registered with DI, allowing tools to request dependencies via method parameters

### The MCP Tool: `search_mtg_cards`

Located in `FancyMCP.Server/Tools/MtgTools.cs`, this is the heart of the MCP server:

```csharp
[McpServerToolType]
public static class MtgTools
{
    [McpServerTool, Description("Search for Magic: The Gathering cards...")]
    public static async Task<string> SearchMtgCards(
        IMtgAiService mtgAiService,      // Injected dependency
        AzureOpenAIClient azureClient,    // Injected dependency
        IConfiguration configuration,     // Injected dependency
        ILogger<Program> logger,          // Injected dependency
        string query)                     // User's search query
    {
        // Implementation...
    }
}
```

**Tool Registration Process:**
1. The `[McpServerToolType]` attribute marks the class for tool discovery
2. The `[McpServerTool]` attribute marks the method as an MCP tool
3. The `Description` attribute provides metadata for clients
4. Parameters are automatically resolved via dependency injection (except the last parameter which is the user input)

### Data Flow Through the Server

#### Step 1: Receive Query
The MCP protocol delivers the user's query to the tool as the `query` parameter.

#### Step 2: Search for Cards
```csharp
List<MtgCard> cards = await mtgAiService.UseDeckAiService(query);
```
This calls into the `MtgAiService`, which wraps the `DeckAiService` from MtgChatBotPrototype. The `DeckAiService`:
- Uses Azure OpenAI's function calling capability
- Analyzes the user's query to determine search parameters
- Calls the Scryfall API to find matching cards
- Returns a `List<MtgCard>` with structured data (name, mana cost, type, text, etc.)

#### Step 3: Generate Natural Language Summary
```csharp
// Serialize cards to JSON for the AI to analyze
string cardsJson = JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true });

// Ask Azure OpenAI to summarize
List<ChatMessage> messages = new List<ChatMessage>
{
    new SystemChatMessage("You are a helpful Magic: The Gathering assistant..."),
    new UserChatMessage($"The user asked: '{query}'\n\nI found these cards:\n{cardsJson}\n\n
                         Please provide a natural language summary...")
};

ChatCompletion response = await chatClient.CompleteChatAsync(messages, chatOptions);
return response.Content[0].Text;
```

**This is the key innovation**: Instead of returning raw JSON, we make a **second Azure OpenAI call** to transform the structured data into a conversational response. This gives us:
- Natural language descriptions
- Context-aware explanations
- Friendly, engaging tone
- Intelligent highlighting of relevant cards

#### Step 4: Return to Client
The natural language response flows back through the MCP protocol to the client.

### Why Two AI Calls?

**First AI Call** (in DeckAiService):
- **Purpose**: Understand the query and find relevant cards
- **Uses**: Function calling to search Scryfall API
- **Returns**: Structured data (`List<MtgCard>`)

**Second AI Call** (in MCP Tool):
- **Purpose**: Transform data into natural language
- **Uses**: Conversation mode with card data as context
- **Returns**: Human-friendly text

This separation allows us to:
- Reuse the existing search logic unchanged
- Add natural language presentation as a layer
- Maintain structured data availability for other uses

## FancyMCP.Console - The MCP Client

### Client Implementation

The console app follows the official MCP C# SDK client patterns:

```csharp
// Create transport that spawns the server
StdioClientTransport clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "FancyMCP.Console",
    Command = "dotnet",
    Arguments = [serverPath],  // Path to FancyMCP.Server.dll
});

// Create and connect the client
McpClient client = await McpClient.CreateAsync(clientTransport);

// Discover available tools
IList<McpClientTool> tools = await client.ListToolsAsync();

// Call a tool
CallToolResult result = await client.CallToolAsync(
    "search_mtg_cards",
    new Dictionary<string, object?> { ["query"] = userInput },
    cancellationToken: CancellationToken.None);
```

### Dedicated Server Instance Management

**Why Each Client Gets Its Own Server:**

The stdio transport model means:
1. **Client starts** → Spawns server process via `dotnet FancyMCP.Server.dll`
2. **Client connects** → Communicates via server's stdin/stdout
3. **Client exits** → Server process terminates automatically

**Benefits:**
- **Isolated state** - Each client session is independent
- **Clean lifecycle** - No orphaned server processes
- **Simple deployment** - No need to manage server startup separately
- **Process boundaries** - Server crashes don't affect client (and vice versa)

### Maintaining Natural Language Communication

The console app is **deliberately simple** because all intelligence lives in the server:

```csharp
// Display the natural language response
foreach (TextContentBlock content in result.Content.OfType<TextContentBlock>())
{
    Console.WriteLine(content.Text);  // That's it!
}
```

**No JSON parsing, no formatting logic, no AI calls** - the client just displays what the server returns.

This demonstrates a key MCP principle: **separation of concerns**. The client handles:
- User input
- MCP protocol communication
- Output display

The server handles:
- Business logic
- AI interactions
- Data processing

### User Experience Flow

1. User types: `"Find me some blue control cards"`
2. Client sends to server via MCP: `CallToolAsync("search_mtg_cards", { query: "..." })`
3. Server processes (see data flow above)
4. Server returns: `"Great! I found some excellent blue control cards for you. Counterspell is..."`
5. Client displays the response
6. User sees natural, conversational text

## Dependency on MtgChatBotPrototype

### Why This Dependency Exists

The **MtgChatBotPrototype** project contains the core card search engine that:
- Implements Azure OpenAI function calling
- Defines the card search logic and prompts
- Handles Scryfall API integration
- Returns structured `MtgCard` objects

**Rather than rewriting this logic**, FancyMCP.Server wraps it and exposes it via MCP.

### Integration Points

#### 1. Service Registration
```csharp
// In FancyMCP.Server/Program.cs
builder.Services.AddSingleton<IDeckAiService>(sp =>
{
    AzureOpenAIClient azureClient = sp.GetRequiredService<AzureOpenAIClient>();
    IMtgApiClient mtgClient = sp.GetRequiredService<IMtgApiClient>();
    IConfiguration config = builder.Configuration;
    return new DeckAiService(azureClient, mtgClient, config);
});
```

#### 2. Wrapper Service
```csharp
// In FancyMCP.Server/Services/MtgAiService.cs
public class MtgAiService : IMtgAiService
{
    private readonly IDeckAiService _deckAiService;
    
    public async Task<List<MtgCard>> UseDeckAiService(string message)
    {
        List<MtgCard> result = await _deckAiService.QueryOpenAiAsync(message);
        return result;
    }
}
```

The `MtgAiService` acts as a **bridge** between the MCP server and the prototype, adapting the interface for dependency injection.

#### 3. Tool Usage
```csharp
// In the MCP tool
List<MtgCard> cards = await mtgAiService.UseDeckAiService(query);
// Returns the results from DeckAiService.QueryOpenAiAsync()
```

### Benefits of This Architecture

- **Code Reuse** - Don't duplicate the complex search logic
- **Separation of Concerns** - Prototype handles search, MCP handles protocol
- **Independent Evolution** - Can update prototype without changing MCP code
- **Layered Design** - MCP adds natural language on top of structured search

### What MtgChatBotPrototype Provides

| Component | Purpose |
|-----------|---------|
| `DeckAiService` | Main search engine with Azure OpenAI function calling |
| `MtgApiClient` | Wrapper for Scryfall API calls |
| `MtgCard` | Data model for card information |
| Search prompts | Pre-configured prompts for AI card search |
| Function definitions | Schema for AI function calling |

### What FancyMCP Adds

| Component | Purpose |
|-----------|---------|
| MCP protocol | Standard interface for AI tool exposure |
| Natural language layer | Second AI call for conversational responses |
| Client management | Stdio transport and lifecycle management |
| Tool registration | Declarative tool definition with attributes |
| Logging | Comprehensive logging for debugging |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure OpenAI API access
- Visual Studio 2022 or later (optional, for CodeLens MCP support)

### Configuration

Create or update `appsettings.json` in the solution root:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "your-deployment-name"
  }
}
```

### Build and Run

```bash
# Build the solution
dotnet build

# Run the console app (automatically starts the MCP server)
dotnet run --project FancyMCP.Console
```

### Using with Claude Desktop or Other MCP Clients

Add to your MCP client configuration (e.g., `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "fancy-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/FancyMCP.Server/FancyMCP.Server.csproj"]
    }
  }
}
```

Or use the included `.mcp.json` file for Visual Studio CodeLens support.

## Verification and Debugging

The MCP server writes detailed logs to `mcp-tool-calls.log` in the server's output directory:

```
=== MCP Tool Called at 2025-01-14 10:45:32 ===
Query: Find me some blue control cards
Cards found: 5
Calling Azure OpenAI to summarize...
SUCCESS: Response generated
```

This log file allows you to verify:
- The MCP tool is being invoked
- The prototype logic is executing
- Cards are being found
- AI summarization is working

## Key Takeaways

### MCP Server Design Patterns

1. **Use stdio transport** for single-client, spawned server scenarios
2. **Attribute-based tool registration** for declarative tool definition
3. **Dependency injection** for clean service composition
4. **Layered responses** - structured data + natural language
5. **Comprehensive logging** for debugging and verification

### MCP Client Design Patterns

1. **Let the client spawn the server** for isolated instances
2. **Keep clients simple** - push intelligence to the server
3. **Use McpClient.CreateAsync()** for proper connection handling
4. **Tool discovery** before use via `ListToolsAsync()`
5. **Type-safe parameters** via dictionaries

### Integration Patterns

1. **Wrap existing services** rather than rewriting them
2. **Adapt interfaces** for dependency injection compatibility
3. **Add value on top** - MCP adds protocol + natural language to existing search
4. **Maintain separation** - prototype handles business logic, MCP handles presentation

## Further Reading

- [Model Context Protocol Official Site](https://modelcontextprotocol.io/)
- [MCP C# SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/)
- [MCP C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)