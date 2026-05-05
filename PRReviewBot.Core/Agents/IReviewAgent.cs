namespace PRReviewBot.Core.Agents;

public interface IReviewAgent
{
    string Name { get; }
    Task<List<ReviewFinding>> ReviewAsync(string diff);
}