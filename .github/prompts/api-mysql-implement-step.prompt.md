---
mode: 'agent'
description: 'Implement ONE planned step (minimal diffs, build checkpoint, no guessing)'
---

We will implement exactly ONE step from an agreed plan for this ASP.NET Core API (MySQL).

Inputs:
- Step to implement: ${input:step:Paste the step number + the step text from the plan}
- Extra details (optional): ${input:details:Any extra constraints or clarifications}

Context policy (IMPORTANT):
- Use only referenced context: @workspace plus the #files/#symbols I provide in this chat.
- If you need additional context, STOP and ask me to attach the specific #file/#symbol/#output you need.
- Prefer minimal, surgical diffs. Do not refactor unrelated code. Do not add new NuGet packages unless explicitly approved.
- Follow existing project conventions (logging, DI, error handling, validation, mapping, async, cancellation tokens).

Task:
1) Confirm what you’re changing and which files are involved.
2) If any required file/symbol is missing, ask for it explicitly (example: “Please attach #Program.cs and #MyDbContext so I can wire DI and migrations correctly.”).
3) Produce the code changes as a per-file patch:
   - Show each changed file with a clear “before/after” or unified diff style.
   - Keep changes small and focused on this step.
4) End with a checkpoint:
   - Exact commands to run (e.g., dotnet build / dotnet test)
   - What output to paste back (use #output) if anything fails

Output format:
- Step recap (1–3 bullets)
- Files changed (bullets)
- Changes (per file)
- Checkpoint commands
- If build/test fails: what to paste (#output)
