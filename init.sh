#!/bin/bash
# /init command - Initializes repository context
# Usage: ./init.sh or source init.sh

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONTEXT_FILE="$REPO_ROOT/context/repo-context.json"

echo "[AGENT] Initializing repository context..."

# Create context directory if it doesn't exist
mkdir -p "$REPO_ROOT/context"

# Gather repository information
PROJECT_NAME=$(basename "$REPO_ROOT")
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
GIT_COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Detect agents
AGENTS_LIST=()
for agent_file in "$REPO_ROOT/agents"/*.md; do
    if [ -f "$agent_file" ]; then
        # Extract agent metadata if present (looking for YAML frontmatter)
        if grep -q "^name: .*agent" "$agent_file" 2>/dev/null; then
            agent_name=$(grep "^name: " "$agent_file" | head -1 | sed 's/name: //' | tr -d '\r' | xargs)
            AGENTS_LIST+=("$agent_name")
        fi
    fi
done

# Count source files
SRC_FILES=$(find "$REPO_ROOT/src" -type f 2>/dev/null | wc -l || echo "0")
LIB_FILES=$(find "$REPO_ROOT/lib" -type f 2>/dev/null | wc -l || echo "0")

# Generate context JSON
cat > "$CONTEXT_FILE" << EOF
{
  "repository": {
    "name": "$PROJECT_NAME",
    "root": "$REPO_ROOT",
    "git": {
      "branch": "$GIT_BRANCH",
      "commit": "$GIT_COMMIT"
    }
  },
  "initialized": "$TIMESTAMP",
  "agents": [
$(IFS=$'\n'; for agent in "${AGENTS_LIST[@]}"; do echo "    \"$agent\""; done | sed '$!s/$/,/')
  ],
  "structure": {
    "src_files": $SRC_FILES,
    "lib_files": $LIB_FILES,
    "agents_count": ${#AGENTS_LIST[@]}
  },
  "status": "initialized"
}
EOF

echo ""
echo "✓ Repository context initialized"
echo "  Location: $CONTEXT_FILE"
echo "  Branch: $GIT_BRANCH"
echo "  Commit: $GIT_COMMIT"
echo "  Agents detected: ${#AGENTS_LIST[@]}"
for agent in "${AGENTS_LIST[@]}"; do
    echo "    - $agent"
done
echo ""
echo "Available commands:"
echo "  ./agent.sh <agent-name> - Execute agent task"
echo "  ./agent.sh list         - List available agents"
echo ""

# Export context path for other scripts
export REPO_CONTEXT_FILE="$CONTEXT_FILE"
