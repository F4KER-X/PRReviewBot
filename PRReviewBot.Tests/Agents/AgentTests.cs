using Moq;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Agents;

public class AgentTests
{
    private readonly Mock<ILlmService> _mockLlm = new();

    [Theory]
    [InlineData(typeof(SecurityAgent), "Security Agent")]
    [InlineData(typeof(CodeQualityAgent), "Code Quality Agent")]
    [InlineData(typeof(PerformanceAgent), "Performance Agent")]
    [InlineData(typeof(ArchitectureAgent), "Architecture Agent")]
    public void Agent_HasExpectedName(Type agentType, string expectedName)
    {
        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        Assert.Equal(expectedName, agent.Name);
    }

    [Theory]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_WithValidJsonResponse_ReturnsFindings(Type agentType)
    {
        const string jsonResponse = """
            [
                {
                    "fileName": "test.cs",
                    "description": "Issue found",
                    "severity": "Warning",
                    "category": "Quality",
                    "suggestion": "Fix it",
                    "lineNumber": 10
                }
            ]
            """;

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        var findings = await agent.ReviewAsync("some diff");

        Assert.Single(findings);
        Assert.Equal(Severity.Warning, findings[0].Severity);
        Assert.Equal("test.cs", findings[0].FileName);
    }

    [Theory]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_WithMarkdownCodeBlock_ParsesCorrectly(Type agentType)
    {
        const string response = "```json\n[{\"fileName\":\"app.cs\",\"description\":\"N+1 query\",\"severity\":\"Issue\",\"category\":\"Performance\",\"suggestion\":\"Eager load\",\"lineNumber\":5}]\n```";

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        var findings = await agent.ReviewAsync("some diff");

        Assert.Single(findings);
        Assert.Equal("app.cs", findings[0].FileName);
    }

    [Theory]
    [InlineData(typeof(SecurityAgent))]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_SendsDiffToLlm(Type agentType)
    {
        const string diff = "=== service.cs ===\n+var x = GetData();";

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        await agent.ReviewAsync(diff);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains(diff))),
            Times.Once);
    }

    [Theory]
    [InlineData(typeof(SecurityAgent))]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_WithMultipleFindings_ReturnsAll(Type agentType)
    {
        const string jsonResponse = """
            [
                {"fileName":"a.cs","description":"Issue 1","severity":"Critical","category":"Security","suggestion":"Fix 1","lineNumber":1},
                {"fileName":"b.cs","description":"Issue 2","severity":"Warning","category":"Quality","suggestion":"Fix 2","lineNumber":2},
                {"fileName":"c.cs","description":"Issue 3","severity":"Suggestion","category":"Architecture","suggestion":"Fix 3","lineNumber":null}
            ]
            """;

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        var findings = await agent.ReviewAsync("some diff");

        Assert.Equal(3, findings.Count);
    }

    [Theory]
    [InlineData(typeof(SecurityAgent))]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_WithEmptyResponse_ReturnsEmptyList(Type agentType)
    {
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        var findings = await agent.ReviewAsync("some diff");

        Assert.Empty(findings);
    }

    [Theory]
    [InlineData(typeof(SecurityAgent))]
    [InlineData(typeof(CodeQualityAgent))]
    [InlineData(typeof(PerformanceAgent))]
    [InlineData(typeof(ArchitectureAgent))]
    public async Task ReviewAsync_WithGarbageResponse_ReturnsEmptyList(Type agentType)
    {
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("I cannot analyze this code.");

        var agent = (IReviewAgent)Activator.CreateInstance(agentType, _mockLlm.Object)!;
        var findings = await agent.ReviewAsync("some diff");

        Assert.Empty(findings);
    }
}
