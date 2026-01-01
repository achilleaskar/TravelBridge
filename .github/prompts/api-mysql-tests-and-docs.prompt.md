---
mode: 'agent'
description: 'Add tests + docs AFTER code works (ASP.NET Core API + MySQL)'
---

We are at the end of a change. Now add tests and documentation.

Inputs:
- Change summary: ${input:summary:Describe what was implemented (routes, behavior changes, DB impact)}
- Target audience: ${input:audience:Who will read the docs? (devs, ops, both)}
- Docs location rule: docs/ (do not create refactor docs)

Rules:
- Only write tests AFTER the code builds and the app works. If it doesn’t, ask for #output and fix first.
- Use the test framework and patterns already in the repo (xUnit/NUnit/MSTest, FluentAssertions, mocking lib, etc.).
- Do NOT introduce new infrastructure (e.g., Testcontainers/Docker-based DB tests) unless the repo already uses it or I explicitly approve.
- Prefer: unit tests for services/validators + lightweight integration tests if the repo already supports them.
- For bug fixes: add a regression test when feasible.

Task A — Verify readiness:
1) Ask me to confirm that `dotnet build` and `dotnet test` are green, or request #output if not.

Task B — Tests:
2) Identify existing test projects and conventions (from @workspace + attached files). If unclear, ask me to attach:
   - The test project csproj
   - One representative existing test class
3) Add/extend tests for the change:
   - Arrange-Act-Assert
   - Stable and parallelizable
   - Avoid real network/disk unless existing patterns do
4) Provide commands to run tests and interpret failures.

Task C — Docs in docs/:
5) Update or create practical docs under docs/ (not refactor notes). Include:
   - Endpoint(s) added/changed (routes, request/response examples)
   - Config/env vars (especially MySQL connection/config keys, feature flags)
   - DB changes (migration name/SQL script location, rollback notes)
   - Troubleshooting section (common errors and how to fix)
6) Keep it short, operational, and consistent with existing docs.

Output format:
- Tests: plan (bullets) + patches (per file) + commands to run
- Docs: file list + full markdown content for each docs/ file
