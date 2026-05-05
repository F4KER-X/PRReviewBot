# PRReviewBot

An AI-powered multi-agent pull request review bot built with ASP.NET Core and Semantic Kernel. It automatically analyzes code changes in GitHub PRs using specialized AI agents and posts detailed review comments.

## Architecture

```
GitHub Webhook (PR opened)
        |
        v
  ASP.NET Core API
        |
        v
  Review Orchestrator
        |
        v
  +-----------+-----------+-----------+-----------+
  | Security  | Code      | Perf      | Arch      |
  | Agent     | Quality   | Agent     | Agent     |
  |           | Agent     |           |           |
  +-----------+-----------+-----------+-----------+
        |           |           |           |
        v           v           v           v
              Semantic Kernel (LLM)
                    |
                    v
            Summarizer Agent
                    |
                    v
         GitHub PR Comment Posted
```

## Agents

| Agent | Focus Area |
|-------|-----------|
| **Security Agent** | SQL injection, hardcoded secrets, XSS, auth issues |
| **Code Quality Agent** | SOLID violations, DRY, naming, error handling |
| **Performance Agent** | N+1 queries, missing async/await, allocations |
| **Architecture Agent** | Dependency injection, separation of concerns, coupling |
| **Summarizer Agent** | Consolidates all findings into a prioritized review |

All agents run in parallel and produce findings with severity levels: Critical, Issue, Warning, Suggestion.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 10)
- **AI Orchestration:** Microsoft Semantic Kernel
- **LLM:** Ollama (local) with DeepSeek Coder v2
- **GitHub Integration:** Octokit
- **Containerization:** Docker, Docker Compose
- **CI/CD:** GitHub Actions
- **Testing:** xUnit, Moq

## Project Structure

```
PRReviewBot/
|-- PRReviewBot.Api/          # Web API, webhook controller, DI setup
|-- PRReviewBot.Core/         # Business logic
|   |-- Agents/               # Agent interfaces, base class, implementations
|   |-- Services/             # GitHub and LLM service abstractions
|   |-- Orchestration/        # Review orchestrator
|-- PRReviewBot.Tests/        # Unit tests
|-- Dockerfile
|-- docker-compose.yml
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Ollama (for local LLM)
- GitHub Personal Access Token (with PR read/write permissions)

### Local Setup

1. Clone the repository:
```bash
git clone https://github.com/F4KER-X/PRReviewBot.git
cd PRReviewBot
```

2. Pull the LLM model:
```bash
ollama pull deepseek-coder-v2:16b
```

3. Set your GitHub token:
```bash
cd PRReviewBot.Api
dotnet user-secrets init
dotnet user-secrets set "GitHub:Token" "your-github-token"
```

4. Run the API:
```bash
dotnet run --project PRReviewBot.Api
```

5. Send a test webhook via Swagger at `http://localhost:5062/swagger`

### Docker Setup

1. Create a `.env` file in the root:
```
GITHUB_TOKEN=your-github-token
GITHUB_WEBHOOK_SECRET=
```

2. Run with Docker Compose:
```bash
docker compose up --build
```

3. Pull the model inside the Ollama container:
```bash
docker exec -it prreviewbot-ollama-1 ollama pull deepseek-coder-v2:16b
```

4. Access Swagger at `http://localhost:8080/swagger`

### Running Tests

```bash
dotnet test
```

## Example Review Output

When the bot reviews a PR, it posts a comment like this:

**AI Code Review**

Summary: 6 findings

- 🔴 **Critical:** SQL injection vulnerability via direct string concatenation
- 🔴 **Critical:** Hardcoded database credentials in source code
- 🟠 **Issue:** Service class directly queries database, violating separation of concerns
- 🟡 **Warning:** Missing async/await on I/O operation
- 🟡 **Warning:** No input validation on user-supplied parameters
- 🔵 **Suggestion:** Consider extracting data access into a repository pattern

## Design Decisions

- **Multi-agent pattern** over single prompt: Each agent specializes in one domain, producing more focused and accurate findings than a single catch-all prompt.
- **Interface-based architecture:** All agents implement `IReviewAgent`, enabling easy extension and testing. New agents can be added without modifying existing code (Open/Closed Principle).
- **Parallel execution:** Agents run concurrently via `Task.WhenAll`, reducing total review time.
- **Fire-and-forget webhook handling:** Returns 200 to GitHub immediately and processes the review asynchronously to avoid webhook timeouts.
- **Semantic Kernel:** Microsoft's AI orchestration library provides a clean abstraction over LLM providers, making it easy to swap between Ollama (local) and cloud-based models.

## CI/CD

The GitHub Actions pipeline runs on every push and PR to `main`:

1. Restore dependencies
2. Build in Release mode
3. Run all unit tests

## License

MIT
