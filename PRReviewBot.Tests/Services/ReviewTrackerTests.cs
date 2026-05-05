using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Services;

public class ReviewTrackerTests
{
    private readonly ReviewTracker _tracker = new();

    [Fact]
    public void HasBeenReviewed_ReturnsFalse_WhenNothingReviewed()
    {
        var result = _tracker.HasBeenReviewed("owner", "repo", 1, "abc123");
        Assert.False(result);
    }

    [Fact]
    public void HasBeenReviewed_ReturnsTrue_AfterMarkReviewed()
    {
        _tracker.MarkReviewed("owner", "repo", 1, "abc123");
        Assert.True(_tracker.HasBeenReviewed("owner", "repo", 1, "abc123"));
    }

    [Fact]
    public void HasBeenReviewed_ReturnsFalse_ForDifferentCommitSha()
    {
        _tracker.MarkReviewed("owner", "repo", 1, "abc123");
        Assert.False(_tracker.HasBeenReviewed("owner", "repo", 1, "def456"));
    }

    [Fact]
    public void HasBeenReviewed_ReturnsFalse_ForDifferentPrNumber()
    {
        _tracker.MarkReviewed("owner", "repo", 1, "abc123");
        Assert.False(_tracker.HasBeenReviewed("owner", "repo", 2, "abc123"));
    }

    [Fact]
    public void HasBeenReviewed_ReturnsFalse_ForDifferentRepo()
    {
        _tracker.MarkReviewed("owner", "repo1", 1, "abc123");
        Assert.False(_tracker.HasBeenReviewed("owner", "repo2", 1, "abc123"));
    }

    [Fact]
    public void HasBeenReviewed_ReturnsFalse_ForDifferentOwner()
    {
        _tracker.MarkReviewed("owner1", "repo", 1, "abc123");
        Assert.False(_tracker.HasBeenReviewed("owner2", "repo", 1, "abc123"));
    }

    [Fact]
    public void MarkReviewed_IsIdempotent_WhenCalledTwice()
    {
        _tracker.MarkReviewed("owner", "repo", 1, "abc123");
        var ex = Record.Exception(() => _tracker.MarkReviewed("owner", "repo", 1, "abc123"));
        Assert.Null(ex);
        Assert.True(_tracker.HasBeenReviewed("owner", "repo", 1, "abc123"));
    }

    [Fact]
    public void MarkReviewed_TracksMultiplePrs()
    {
        _tracker.MarkReviewed("owner", "repo", 1, "sha1");
        _tracker.MarkReviewed("owner", "repo", 2, "sha2");

        Assert.True(_tracker.HasBeenReviewed("owner", "repo", 1, "sha1"));
        Assert.True(_tracker.HasBeenReviewed("owner", "repo", 2, "sha2"));
        Assert.False(_tracker.HasBeenReviewed("owner", "repo", 1, "sha2"));
    }

    [Fact]
    public async Task MarkReviewed_IsThreadSafe()
    {
        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => _tracker.MarkReviewed("owner", "repo", i, "sha")));

        await Task.WhenAll(tasks);

        for (var i = 0; i < 50; i++)
            Assert.True(_tracker.HasBeenReviewed("owner", "repo", i, "sha"));
    }
}
