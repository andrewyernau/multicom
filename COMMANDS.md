# Agent Commands Reference

This repository includes a system of commands to initialize repository context and execute agent-specific tasks.

## Quick Start

### 1. Initialize Repository Context

```bash
./init.sh
```

This command:
- Creates a repository context file in `context/repo-context.json`
- Detects all available agents in the `agents/` directory
- Gathers repository metadata (git branch, commit, file counts)
- Displays available commands and detected agents

### 2. List Available Agents

```bash
./agent.sh list
```

Shows all agents configured in the repository with their descriptions, models, and file locations.

### 3. Execute an Agent

```bash
./agent.sh <agent-name>
```

Loads and displays the specific agent's instructions and responsibilities.

**Examples:**
```bash
./agent.sh architect          # Execute architecture agent
./agent.sh documentation      # Execute documentation agent
./agent.sh tdd                # Execute TDD agent
./agent.sh security           # Execute security agent
./agent.sh domain             # Execute domain agent
./agent.sh ux-ui              # Execute UX/UI agent
```

Note: You can use the agent name with or without the `-agent` suffix (e.g., both `architect` and `architect-agent` work).

## Available Agents

The repository currently includes the following agents:

| Agent | Description | Model | File |
|-------|-------------|-------|------|
| **architect-agent** | Responsible for system architecture and codebase structure | opus | ARQUITECT.md |
| **documentation-agent** | Keeps project documentation consistent and synchronized | sonnet | DOCUMENTATOR.md |
| **domain-agent** | Ensures domain model consistency with functional requirements | sonnet | DOMAIN.md |
| **security-agent** | Ensures application security and privacy | sonnet | SECURITY.md |
| **tdd-agent** | Responsible for enforcing Test-Driven Development practices | sonnet | TDD.md |
| **ux-ui-agent** | Ensures visual and experiential coherence | sonnet | UI.md |

## Repository Context

The `context/repo-context.json` file contains:
- Repository metadata (name, root path, git information)
- List of detected agents
- File structure statistics
- Initialization timestamp

This context is used by agents to understand the repository state and perform their tasks effectively.

## Agent Definition Format

Agents are defined in Markdown files with YAML frontmatter:

```markdown
---
name: agent-name
description: Agent description
model: opus|sonnet
tools:
  - tool1
  - tool2
---

Agent capabilities and instructions go here...
```

## Workflow

1. **Initialize** - Run `./init.sh` to set up the repository context
2. **Explore** - Use `./agent.sh list` to see available agents
3. **Execute** - Run `./agent.sh <agent>` to load specific agent tasks
4. **Review** - The AI assistant will execute the agent's responsibilities

## Additional Commands

```bash
./agent.sh help       # Show usage information
./agent.sh --help     # Show usage information
./agent.sh -h         # Show usage information
```

---

For more information about agents, see [AGENTS.md](./AGENTS.md).
