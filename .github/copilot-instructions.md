# GitHub Copilot custom instructions (repo-wide)

You are helping with a .NET (C#) ASP.NET Core API that uses a MySQL database.

## Non-negotiables
- Follow **existing repo conventions** first (architecture, DI, logging, validation, error handling, data access).
- Prefer **minimal, surgical diffs**. Do not refactor unrelated code.
- Do **not** add new NuGet packages, change TFM/SDK/LangVersion, or restructure projects unless explicitly asked.
- Never claim you “read the whole project”. Use @workspace for structure, but request specific context as needed.
- never delete existing comments in code if they are not invalid anymore. if they need update then update them. 

## Plan-first for “big” work
If the request is likely big (more than ~3 files, new endpoints/public APIs, auth, DB/schema changes, cross-project):
1) Produce a concise plan (3–8 steps) **before** writing code.
2) List the exact `#files` / `#symbols` / `#output` you need to proceed.
3) Implement **one step at a time** with a build/test checkpoint.

## Context policy
- If context is missing, ask for the **specific** file/symbol/output needed instead of guessing.
- Prefer referencing: `#Program.cs`, `#<Controller>`, `#<Service>`, `#appsettings.json`, `#output`.

## Validate continuously
- After each step, provide the exact commands to run (usually `dotnet build` and/or `dotnet test`).
- If something fails, ask me to paste the relevant `#output` and then fix it.

## MySQL / DB changes
- Use the repo’s existing approach (EF Core migrations vs SQL scripts vs Dapper/raw SQL).
- Keep changes backward-compatible when feasible.
- Consider indexes and query performance for new access patterns.

## Documentation (feature/ops only)
- When behavior/config/DB/ops changes, update docs under `docs/` (no refactor notes).
- Follow: `docs/ai/documentation-guidelines.md`

## Detailed guidance (consult when relevant)
- `docs/ai/coding-standards.md`
- `docs/ai/testing-guidelines.md`
- `docs/ai/workflows.md`