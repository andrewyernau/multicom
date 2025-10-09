# Agent rules
Purpose: a short, practical guide explaining how an AI agent should operate on this repository.

## Main rules and conventions

- **Language and format**
    - Use English for messages and created files unless the user requests another language.
    - Naming: `camelCase` for non-public identifiers; `SCREAMING_SNAKE_CASE` for global constants and `PascalCase` for public identifiers.

- **Security and privacy**
    - NEVER exfiltrate credentials or secrets. If you detect secrets, redact them and report their location to the user.
    - Sanitize any user-visible text before writing it to public files.

- **Code quality and documentation**
    - Keep a clear, modular file hierarchy.
    - Document public functions (C# XML doc style or equivalent) and include type signatures where applicable.
    - Add minimal unit tests for any public behavior you change or introduce.

- **Error handling and logging**
    - Use descriptive error messages prefixed with `[AGENT]`.
    - Avoid dumping long stack traces into README files; keep detailed logs separate.

- **Interactions with the repository**
    - Before editing: read the relevant files and summarize planned changes briefly.
    - Make small, atomic commits with clear messages.
    - Do not create remote branches or perform deployments without explicit permission.

## Ignore (skip) list

- Skip irrelevant or transient folders unless explicitly required: `.vs/`, `bin/`, `obj/`, `packages/`, `.idea/`, etc.
- Skip `.png` files hence agents cannot visualize images, all images are Mermaid diagrams described on the `.md` files.

## Project layout (recommended)

- `src/` — source code
- `tests/` — unit tests
- `context/` — shared or contextual data (e.g., `.sql`, `.json` files)

Each module or namespace should represent a single responsibility. Avoid deep or ambiguous nesting.

## Naming and examples

Use `PascalCase` for public identifiers (classes, methods, properties) and `camelCase` for locals and private fields. Use `SCREAMING_SNAKE_CASE` for constants.

Example:

```csharp
private const int MAX_RETRIES = 3;

public class FeatureExtractor
{
        private readonly ILogger logger;

        public FeatureExtractor(ILogger logger)
        {
                this.logger = logger;
        }

        /// <summary>
        /// Computes feature vectors from PGN game text.
        /// </summary>
        /// <param name="pgnText">PGN input string representing a chess game.</param>
        /// <returns>A dictionary mapping feature names to numeric values.</returns>
        /// <exception cref="ArgumentException">[AGENT] pgnText cannot be null or empty.</exception>
        public Dictionary<string, double> ComputeFeatures(string pgnText)
        {
                if (string.IsNullOrWhiteSpace(pgnText))
                        throw new ArgumentException("[AGENT] pgnText cannot be null or empty.");

                // Implementation
                return new Dictionary<string, double>();
        }
}
```

## Testing

Provide minimal but complete unit tests for every new or modified public method. Use a consistent testing framework (xUnit, NUnit, or MSTest) and mirror the source layout under `tests/`.

## Error handling and logging

Use descriptive, concise error messages starting with `[AGENT]`. Avoid exposing sensitive data in exception messages or logs.

Example:

```csharp
throw new InvalidOperationException("[AGENT] Unexpected null value during feature extraction.");
```

Use a structured logger (e.g., `ILogger` or Serilog). Keep detailed logs in separate log files; never dump stack traces into docs. Include context identifiers where useful.

## Recommended workflow for non-trivial tasks

1. Read and understand relevant files and the `context/` folder.
2. Create a concise TODO list and mark exactly one item as `in-progress`.
3. Implement minimal, focused changes.
4. Run quick verifications (tests, build checks) and fix obvious issues.
5. Document changes in the commit message and update `CHANGELOG.md` or `README.md` if necessary.

## Ambiguity and safety

- If essential information is missing, infer 1–2 reasonable assumptions, document them, and proceed.
- If the change risks infrastructure, data, or security, stop and request confirmation.
- Prefer non-destructive, reversible edits.

## Optional enhancements

- Provide a pre-commit checklist to validate formatting and run tests.
- Add a GitHub Action to run linting and unit tests on PRs.
- Add a `CONTRIBUTING.md` template to align external contributions with these rules.

---