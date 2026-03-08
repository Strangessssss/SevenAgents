# SevenAgents

A small .NET agent framework and sample project for managing multiple AI agents and services. This repository contains core agent abstractions, an Anthropic service integration, messaging structures, and a lightweight MCP helper set.

## Project summary
SevenAgents provides:
- Agent base types and configs (`SevenAgents/Agents/Agent.cs`, `SevenAgents/Agents/AgentConfig.cs`)
- Anthropic integration (`SevenAgents/Anthropic/AnthropicService.cs`)
- Messaging primitives (`SevenAgents/Messages/Message.cs`, `SevenAgents/Messages/Role.cs`)
- Tools for MCP client/server management (`SevenAgents/MCP/`)

Intended for experimentation and local deployments. Not a production-ready service.

## Prerequisites
- macOS (or Linux/Windows with equivalent tooling)
- .NET SDK 8+ (or the version indicated by `global.json` if present)
- Git (for source control)

## Quick start — macOS (zsh)
Clone and inspect:

```bash
git clone git@github.com:Strangessssss/SevenAgents.git
cd SevenAgents
```

Build the project:

```bash
dotnet build ./SevenAgents/SevenAgents.csproj -c Release
```

Run the project (example):

```bash
dotnet run --project ./SevenAgents/SevenAgents.csproj
```

Run tests (if present):

```bash
# Adjust path if tests are added later
dotnet test
```

## Project structure
- `SevenAgents/` — main project source
  - `Agents/` — agent base classes and configs
  - `Anthropic/` — Anthropic service and models
  - `MCP/` — client/server utilities
  - `Messages/` — message models
  - `Prompts/` — prompt helpers
- `README.md` — this file

## Development notes
- Use `dotnet build` and `dotnet run` for local development.
- Keep configuration (API keys, secrets) out of source. Use environment variables or local secret files excluded by `.gitignore`.

## Contributing
1. Fork the repo and create a feature branch: `git checkout -b feat/describe-change`
2. Make changes, add tests, and run `dotnet test`.
3. Commit with a clear message and push: `git push origin feat/describe-change`
4. Open a PR against `master` and request review.

Please follow the existing coding style and add unit tests for new logic.

## License
Choose a license for this repository and/or add a `LICENSE` file. A common choice is the MIT License.

## Security & Sensitive Files — cautions
- Never commit API keys or secrets. Check:
  - Any `.env`, `appsettings.*.json`, or similar files
  - Files in `SevenAgents/Anthropic/` or config classes (e.g., `AnthropicServiceConfig`)
- If a secret was accidentally committed, remove it and rotate the secret. Use tools like `git filter-repo` or BFG to purge sensitive history.

## Contact
For questions or issues, open an issue on the repo or contact the maintainers listed in the project.
