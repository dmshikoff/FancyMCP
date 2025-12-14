using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

Console.WriteLine("=== Magic:The Gathering Card Wizard ===");
Console.WriteLine();

// Get the path to the MCP server
string serverPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FancyMCP.Server", "bin", "Debug", "net10.0", "FancyMCP.Server.dll"));
string? serverDir = Path.GetDirectoryName(serverPath);
string logPath = Path.Combine(serverDir!, "mcp-tool-calls.log");

if (!File.Exists(serverPath))
{
    Console.WriteLine($"Error: Could not find MCP server at {serverPath}");
    Console.WriteLine("Please build FancyMCP.Server first using 'dotnet build'");
    return;
}

Console.WriteLine("Starting Mystical Card Protocol (MCP) server process...");

// Create the client transport with stdio
StdioClientTransport clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "FancyMCP.Console",
    Command = "dotnet",
    Arguments = [serverPath],
});

Console.WriteLine("Connecting to Mystical Card Protocol (MCP) server...");

// Create and connect the MCP client
McpClient client = await McpClient.CreateAsync(clientTransport);

Console.WriteLine("Bound successfully!");
Console.WriteLine();

// List available tools
IList<McpClientTool> tools = await client.ListToolsAsync();

// Interactive loop
Console.WriteLine("What dark and powerful knowledge do you require? (or 'quit' to exit):");
Console.WriteLine("Example: 'Find me some blue control cards'\n");

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        // Call the search_mtg_cards tool
        CallToolResult result = await client.CallToolAsync(
            "search_mtg_cards",
            new Dictionary<string, object?> { ["query"] = input },
            cancellationToken: CancellationToken.None);

        Console.WriteLine();
        
        // Display the natural language response from the AI
        foreach (TextContentBlock content in result.Content.OfType<TextContentBlock>())
        {
            Console.WriteLine(content.Text);
        }
        
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[Client] Error: {ex.Message}\n");
    }
}

Console.WriteLine("\n[Client] Shutting down...");
Console.WriteLine($"[Client] Final server log available at: {logPath}");
