using Microsoft.SemanticKernel;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Orchestration;
using PRReviewBot.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Semantic Kernel setup
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId: builder.Configuration["Ollama:Model"] ?? "deepseek-coder-v2:16b",
        endpoint: new Uri(builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1"),
        apiKey: "ollama")
    .Build();

builder.Services.AddSingleton(kernel);

//Services
builder.Services.AddSingleton<ILlmService, LlmService>();
builder.Services.AddSingleton<IReviewTracker, ReviewTracker>();
builder.Services.AddSingleton<IGitHubService>(sp =>
    new GitHubService(builder.Configuration["GitHub:Token"]
                      ?? throw new InvalidOperationException("GitHub:Token is required")));

// Agents
builder.Services.AddSingleton<IReviewAgent, SecurityAgent>();
builder.Services.AddSingleton<IReviewAgent, CodeQualityAgent>();
builder.Services.AddSingleton<IReviewAgent, PerformanceAgent>();
builder.Services.AddSingleton<IReviewAgent, ArchitectureAgent>();
builder.Services.AddSingleton<SummarizerAgent>();

// Orchestrator
builder.Services.AddSingleton<ReviewOrchestrator>();


var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }
