# FancyMCP Console Client

A console application that connects to the FancyMCP.Service MCP server to search for Magic: The Gathering cards using AI.

## Overview

This console app demonstrates how to use the [Model Context Protocol (MCP) C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) to create a client that connects to an MCP server. It uses the official `McpClient` API to communicate with the FancyMCP.Service server via stdio transport.

## Setup

1. **Build the Projects**
   
   Make sure both FancyMCP.Service and FancyMCP.Console projects are built:
   ```bash
   dotnet build
   ```

   The console app will automatically use the shared `appsettings.json` from the solution root for Azure OpenAI configuration (this is used by the MCP server).

## Usage

1. **Run the Console App**
   
   ```bash
   dotnet run --project FancyMCP.Console
   ```

   The app will automatically:
   - Start the FancyMCP.Service MCP server as a child process
   - Connect to it using stdio transport
   - List available tools from the server

2. **Enter Your Queries**
   
   Once connected, you can enter natural language queries to search for Magic: The Gathering cards:

   ```
   > Find me some blue control cards
   > Show me powerful red creatures
   > Get cards with flying ability
   ```

3. **Exit**
   
   Type `quit` to exit the application.

## Example Session

```
=== FancyMCP Console Client ===

Connecting to MCP server...
Connected successfully!

Available tools:
  - search_mtg_cards: Search for Magic: The Gathering cards by description

Enter your messages (or 'quit' to exit):
Example: 'Find me some blue control cards'

> Find me some blue control cards

Searching...

Results:
[
  {
    "Name": "Counterspell",
    "ManaCost": "UU",
    "Type": "Instant",
    ...
  },
  ...
]

> quit

Shutting down...
```

## How It Works

The console app follows the official MCP C# SDK pattern:

1. **Creates a StdioClientTransport** - Spawns the FancyMCP.Service process and connects to it via stdin/stdout
2. **Creates an McpClient** - Uses `McpClient.CreateAsync()` to instantiate and connect to the server
3. **Lists Available Tools** - Calls `ListToolsAsync()` to discover what tools the server provides
4. **Calls Tools** - Uses `CallToolAsync()` to invoke the `search_mtg_cards` tool with user queries
5. **Displays Results** - Extracts `TextContentBlock` items from the tool result and displays them

This demonstrates the proper way to implement an MCP client using the official C# SDK, following the patterns documented at [https://github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk).

## Key Differences from Direct Service Usage

Unlike directly consuming the services (which would require referencing all the project dependencies), this approach:

- ? Uses the **official MCP protocol** for communication
- ? Works with **any MCP-compliant server**, not just this one
- ? Demonstrates **proper client implementation** following SDK guidelines
- ? Provides **clean separation** between client and server
- ? Can be easily adapted to connect to **other MCP servers**

This is the recommended approach for building MCP clients in C#.
