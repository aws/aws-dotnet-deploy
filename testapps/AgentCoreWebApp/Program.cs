using AWS.AgentCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddAgentCore(options =>
{
    options.ModelId = "global.anthropic.claude-sonnet-4-6";
});

var app = builder.Build();

app.MapAgentCore<PromptRequest>(
    async (PromptRequest request, Microsoft.Agents.AI.AIAgent agent, CancellationToken ct) =>
    {
        var session = await agent.CreateSessionAsync(cancellationToken: ct);
        var response = await agent.RunAsync(
            request.Prompt ?? "Say hello in one sentence.",
            session: session,
            cancellationToken: ct);
        return response.ToString();
    });

app.Run();

public record PromptRequest(string? Prompt);
