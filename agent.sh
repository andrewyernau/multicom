#!/bin/bash
# /agent command - Execute agent-specific tasks
# Usage: ./agent.sh <agent-name> [args...]
# Example: ./agent.sh architect

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONTEXT_FILE="${REPO_CONTEXT_FILE:-$REPO_ROOT/context/repo-context.json}"
AGENT_REQUESTED="$1"

# Function to display usage
show_usage() {
    echo "Usage: ./agent.sh <agent-name> [args...]"
    echo ""
    echo "Available commands:"
    echo "  ./agent.sh list                - List all available agents"
    echo "  ./agent.sh <agent-name>        - Execute specific agent task"
    echo ""
    echo "Examples:"
    echo "  ./agent.sh architect           - Run architecture analysis"
    echo "  ./agent.sh documentation       - Update documentation"
    echo "  ./agent.sh tdd                 - Generate TDD tests"
    exit 1
}

# Function to list available agents
list_agents() {
    echo "[AGENT] Available agents in repository:"
    echo ""
    
    for agent_file in "$REPO_ROOT/agents"/*.md; do
        if [ -f "$agent_file" ]; then
            if grep -q "^name: .*agent" "$agent_file" 2>/dev/null; then
                agent_name=$(grep "^name: " "$agent_file" | head -1 | sed 's/name: //' | tr -d '\r' | xargs)
                agent_desc=$(grep "^description: " "$agent_file" | head -1 | sed 's/description: //' | tr -d '\r' | xargs || echo "No description")
                agent_model=$(grep "^model: " "$agent_file" | head -1 | sed 's/model: //' | tr -d '\r' | xargs || echo "unknown")
                
                echo "  ◆ $agent_name"
                echo "    Description: $agent_desc"
                echo "    Model: $agent_model"
                echo "    File: $(basename "$agent_file")"
                echo ""
            fi
        fi
    done
}

# Function to find agent file
find_agent_file() {
    local agent_name="$1"
    
    # Normalize: add -agent suffix if not present
    local normalized_name="$agent_name"
    if [[ ! "$agent_name" =~ -agent$ ]]; then
        normalized_name="${agent_name}-agent"
    fi
    
    # Try exact match with different patterns in agents directory
    for pattern in "${agent_name^^}.md" "${normalized_name^^}.md"; do
        if [ -f "$REPO_ROOT/agents/$pattern" ]; then
            echo "$REPO_ROOT/agents/$pattern"
            return 0
        fi
    done
    
    # Try case-insensitive search in agents directory
    for agent_file in "$REPO_ROOT/agents"/*.md; do
        if [ -f "$agent_file" ]; then
            file_agent_name=$(grep "^name: " "$agent_file" 2>/dev/null | head -1 | sed 's/name: //' | tr -d '\r' | xargs || echo "")
            # Match full name with or without -agent suffix
            if [ "${file_agent_name,,}" = "${agent_name,,}" ] || [ "${file_agent_name,,}" = "${normalized_name,,}" ]; then
                echo "$agent_file"
                return 0
            fi
        fi
    done
    
    return 1
}

# Function to execute agent task
execute_agent() {
    local agent_name="$1"
    shift  # Remove agent name from arguments
    
    local agent_file=$(find_agent_file "$agent_name")
    
    if [ -z "$agent_file" ]; then
        echo "[AGENT] Error: Agent '$agent_name' not found"
        echo ""
        echo "Run './agent.sh list' to see available agents"
        exit 1
    fi
    
    echo "[AGENT] Executing agent: $agent_name"
    echo "[AGENT] Agent file: $(basename "$agent_file")"
    echo ""
    
    # Extract agent metadata
    local agent_description=$(grep "^description: " "$agent_file" | head -1 | sed 's/description: //' | tr -d '\r' | xargs || echo "")
    local agent_model=$(grep "^model: " "$agent_file" | head -1 | sed 's/model: //' | tr -d '\r' | xargs || echo "sonnet")
    
    echo "Description: $agent_description"
    echo "Model: $agent_model"
    echo ""
    echo "---"
    echo ""
    
    # Read agent instructions
    echo "[AGENT] Loading agent instructions from $agent_file..."
    echo ""
    
    # Display the agent's full content (skipping YAML frontmatter between --- markers)
    tr -d '\r' < "$agent_file" | awk 'BEGIN{p=0} /^---$/{if(++c==2){p=1;next}} p'
    
    echo ""
    echo "---"
    echo ""
    echo "[AGENT] Agent '$agent_name' task definition loaded."
    echo "[AGENT] Repository context: $CONTEXT_FILE"
    echo ""
    echo "Next steps:"
    echo "  1. Review the agent's responsibilities above"
    echo "  2. The AI assistant should now execute the agent's specific tasks"
    echo "  3. Results will be generated according to agent specifications"
    echo ""
}

# Check if context is initialized
if [ ! -f "$CONTEXT_FILE" ]; then
    echo "[AGENT] Warning: Repository context not initialized"
    echo "[AGENT] Run './init.sh' first to initialize the repository context"
    echo ""
fi

# Main command routing
case "$AGENT_REQUESTED" in
    "")
        show_usage
        ;;
    "list"|"ls"|"-l"|"--list")
        list_agents
        ;;
    "help"|"-h"|"--help")
        show_usage
        ;;
    *)
        execute_agent "$@"
        ;;
esac
