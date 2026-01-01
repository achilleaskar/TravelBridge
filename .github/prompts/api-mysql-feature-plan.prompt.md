---
mode: 'agent'
description: 'Plan a change for an ASP.NET Core API using MySQL (plan-first, context-first, step-by-step)'
---

You are helping me implement a change in a .NET (C#) ASP.NET Core API that uses a MySQL database.

Feature request:
- ${input:feature:Describe the feature/bugfix you want (one paragraph)}

Constraints / rules:
- ${input:constraints:Any constraints? (e.g., no new NuGet packages, must be backward compatible, etc.)}

Definition of done (what should be true at the end):
- ${input:done:List 3-8 bullet points}

Context policy (IMPORTANT):
- Do NOT claim you “read the whole project”.
- Use @workspace for high-level structure, but ask me to attach the specific files/symbols you need (e.g., #Program.cs, #SomeController, #SomeService, #SomeDbContext, #appsettings.json, #output).
- If the change seems “big” (likely >3 files, new endpoints/public API, auth, DB/schema changes, cross-project): plan FIRST and ask for missing context BEFORE writing code.

Planning task:
1) Identify (from @workspace) what architecture this solution uses:
   - Controllers vs Minimal APIs
   - DI registration style
   - Data access style (EF Core DbContext/migrations vs Dapper/raw SQL vs repository pattern)
   - Validation + error handling conventions
   If you cannot determine these from @workspace alone, ask me to attach the minimum set of files needed (be specific).

2) Produce a concise plan (3–8 steps) that is safe and incremental.
   - Each step should list: files likely touched, what changes, and a build/test checkpoint.
   - Include DB considerations: schema changes/migrations (ONLY if the repo already uses them), indexes, backward compatibility, and rollout plan.

3) For each step, list exactly what you need from me as context (which #files, #symbols, or #output logs).

Output format:
- Assumptions (bullets)
- Missing context I need (bullets, with #file/#symbol examples)
- Proposed plan (numbered steps)
- Risks & mitigations (bullets)
- Tests to add at the end (bullets; respect existing test framework/patterns)
- Docs to update (bullets; place under docs/)
