# Documentation guidelines (docs/)

This repository keeps **practical** documentation for features and operations.  
We do **not** keep “refactor diaries” or narrative rewrite notes.

## When to update docs
Update or add docs under `docs/` when any of these change:
- Public API behavior (routes, request/response, status codes, error shapes)
- Configuration / environment variables (including MySQL settings)
- Database schema / migrations / scripts
- Operational behavior (jobs, background workers, retries, timeouts)
- Troubleshooting steps for a common failure

## Where to put docs
Recommended structure:
- `docs/api/` — endpoints, examples, auth, versioning
- `docs/config/` — configuration keys, env vars, sample values
- `docs/db/` — migrations/scripts, rollback notes, indexes
- `docs/ops/` — runbooks, monitoring, troubleshooting
- `docs/decisions/` — short decision notes when something non-obvious is chosen

If the repo already has a different structure, follow it.

## Style
- Keep docs short and actionable.
- Prefer examples:
  - curl examples
  - JSON request/response
  - config snippets
- Include “how to run locally” only if it changed (don’t duplicate README unnecessarily).
- If you introduce a non-obvious decision, add a short note: context → decision → consequences.

## Docs template for a new endpoint
Include:
- Purpose
- Route + method
- Request schema + example
- Response schema + examples (success + failure)
- Validation rules
- Auth requirements (if any)
- Related config + DB impact
- Troubleshooting section