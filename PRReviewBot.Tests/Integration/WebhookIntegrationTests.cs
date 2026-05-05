using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Integration;

public class WebhookIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IGitHubService> _githubMock = new();
    private readonly Mock<ILlmService> _llmMock = new();

    public WebhookIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _githubMock
            .Setup(x => x.GetPullRequestDiffAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(string.Empty);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var githubDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGitHubService));
                if (githubDescriptor != null) services.Remove(githubDescriptor);
                services.AddSingleton<IGitHubService>(_githubMock.Object);

                var llmDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILlmService));
                if (llmDescriptor != null) services.Remove(llmDescriptor);
                services.AddSingleton<ILlmService>(_llmMock.Object);
            });
        });
    }

    [Fact]
    public async Task Post_WithOpenedAction_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = BuildPayload("opened", 1, "sha-opened-test");

        var response = await client.PostAsJsonAsync("/api/webhook/github", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithSynchronizeAction_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = BuildPayload("synchronize", 2, "sha-sync-test");

        var response = await client.PostAsJsonAsync("/api/webhook/github", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithClosedAction_Returns200WithIgnoredMessage()
    {
        var client = _factory.CreateClient();
        var payload = BuildPayload("closed", 3, "sha-closed-test");

        var response = await client.PostAsJsonAsync("/api/webhook/github", payload);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ignored", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_WithMissingRepository_Returns400()
    {
        var client = _factory.CreateClient();
        var payload = new { action = "opened", number = 4, pull_request = new { head = new { sha = "abc" } } };

        var response = await client.PostAsJsonAsync("/api/webhook/github", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_SamePrAndCommitTwice_SecondReturnsAlreadyReviewed()
    {
        var client = _factory.CreateClient();
        var payload = BuildPayload("opened", 99, "sha-dedup-unique");

        var first = await client.PostAsJsonAsync("/api/webhook/github", payload);
        var second = await client.PostAsJsonAsync("/api/webhook/github", payload);

        var secondContent = await second.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("Already reviewed", secondContent);
    }

    [Fact]
    public async Task Post_SamePrDifferentCommit_BothProcessed()
    {
        var client = _factory.CreateClient();
        var firstPayload = BuildPayload("opened", 50, "sha-commit-1");
        var secondPayload = BuildPayload("synchronize", 50, "sha-commit-2");

        var first = await client.PostAsJsonAsync("/api/webhook/github", firstPayload);
        var second = await client.PostAsJsonAsync("/api/webhook/github", secondPayload);

        var secondContent = await second.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.DoesNotContain("Already reviewed", secondContent);
    }

    [Fact]
    public async Task Post_WithInvalidSignature_Returns401()
    {
        const string secret = "configured-secret";
        var factoryWithSecret = _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["GitHub:WebhookSecret"] = secret })));

        var client = factoryWithSecret.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/github");
        request.Headers.Add("X-Hub-Signature-256", "sha256=invalidsignature");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithValidSignature_Returns200()
    {
        const string secret = "integration-test-secret";
        var payload = BuildPayload("opened", 77, "sha-sig-test");
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var signature = ComputeSignature(secret, payloadJson);

        var apiFactory = _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["GitHub:WebhookSecret"] = secret })));

        var client = apiFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/github");
        request.Headers.Add("X-Hub-Signature-256", signature);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static object BuildPayload(string action, int number, string sha) => new
    {
        action,
        number,
        pull_request = new { head = new { sha } },
        repository = new { name = "repo", owner = new { login = "owner" } }
    };

    private static string ComputeSignature(string secret, string body)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
