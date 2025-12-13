using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

Console.WriteLine("=== FancyMCP Console Client ===");
Console.WriteLine();

// Get the path to the MCP server
var serverPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FancyMCP.Service", "bin", "Debug", "net10.0", "FancyMCP.Service.dll"));

if (!File.Exists(serverPath))
{
    Console.WriteLine($"Error: Could not find MCP server at {serverPath}");
    Console.WriteLine("Please build FancyMCP.Service first using 'dotnet build'");
    return;
}

Console.WriteLine("Connecting to MCP server...");

// Create the client transport with stdio
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "FancyMCP.Console",
    Command = "dotnet",
    Arguments = [serverPath],
});

// Create and connect the MCP client
var client = await McpClient.CreateAsync(clientTransport);

Console.WriteLine("Connected successfully!\n");

// List available tools
var tools = await client.ListToolsAsync();
Console.WriteLine("Available tools:");
foreach (var tool in tools)
{
    Console.WriteLine($"  - {tool.Name}: {tool.Description}");
}
Console.WriteLine();

// Interactive loop
Console.WriteLine("Enter your messages (or 'quit' to exit):");
Console.WriteLine("Example: 'Find me some blue control cards'\n");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

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
        Console.WriteLine("\nSearching...");

        // Call the search_mtg_cards tool
        var result = await client.CallToolAsync(
            "search_mtg_cards",
            new Dictionary<string, object?> { ["query"] = input },
            cancellationToken: CancellationToken.None);

        Console.WriteLine("\nResults:");
        
        // Display the content from the tool result
        foreach (var content in result.Content.OfType<TextContentBlock>())
        {
            Console.WriteLine(content.Text);
        }
        
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.Message}\n");
    }
}

Console.WriteLine("\nShutting down...");
