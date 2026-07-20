# Tool entry points

The following locations intentionally remain outside `docs` because tools discover them by name and root-relative location:

| Path | Consumer / reason |
| --- | --- |
| `.github/workflows` | GitHub Actions workflow discovery |
| `.github/copilot-instructions.md` | Repository-wide GitHub Copilot instructions |
| `.github/skills` | GitHub/Copilot-compatible skill discovery generated for this repository |
| `.agents/skills` | Codex/agent skill discovery generated for this repository |
| `.claude/skills` | Claude skill discovery generated for this repository |
| `.codex/config.toml` | Codex repository configuration and Aspire MCP registration |
| `.mcp.json` | Root MCP configuration used by compatible clients |
| `.vscode/mcp.json` | VS Code MCP server configuration |
| `.copilotinstructions.md` | Existing compatibility entry point for clients that still read this filename |

The three skill trees are byte-for-byte equivalent at the time of this audit. They are intentionally not merged or replaced with symlinks because the clients do not share one guaranteed discovery convention and Windows/Linux symlink behavior would add risk. They should be regenerated together by the owning tool when Aspire updates them, rather than edited as general project documentation.

The generated trees referred to an optional `aspireify` sibling that is not installed in this repository. Those broken local links were converted to conditional plain-text routing in all three copies; no replacement configuration mechanism or missing skill was invented.

The previously misplaced Copilot entry point under `src` was moved to the repository-root `.github` directory because the repository, not `src`, is the tool discovery root. Its reusable content now lives in [project-guidance.md](project-guidance.md).
