# Copilot workflows for this repo

## Golden workflow (for anything non-trivial)
1) **Plan first** if the change is big (likely >3 files, new endpoint/public API, auth, DB changes, cross-project).
2) Ask for the **exact context** needed (specific `#files`, `#symbols`, or `#output`).
3) Implement in **small diffs** (one step at a time).
4) Run **build/tests** between steps (`dotnet build`, `dotnet test`).
5) Add/extend **tests at the end** (unless explicitly doing TDD).
6) Update **docs/** for behavior/config/db/ops changes (not refactor notes).

## Prompt library (recommended)
These prompt files live in `.github/prompts/`:
- `api-mysql-feature-plan.prompt.md` — plan-first + ask for missing context
- `api-mysql-implement-step.prompt.md` — implement one step with minimal diffs
- `api-mysql-tests-and-docs.prompt.md` — tests + docs after code works

### Using prompt files in Visual Studio
In Copilot Chat, type `#prompt:` and select one of the prompt files (or attach it via the context/attachment UI).
Prompt files are supported in Visual Studio and are currently in preview.  

## Context references you should use
- `@workspace` for high-level structure
- `#SomeFile.cs` for a specific file
- `#SomeSymbolName` for a class/method
- `#output` for build/test output

## What to paste when something fails
- Build errors: Output Window → Build
- Test failures: Output Window → Tests (or test runner output)
- Runtime issues: exception + stack trace + relevant logs