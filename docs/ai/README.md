# AI customization docs

This folder contains **detailed** guidance for Copilot and humans.
The repo-wide always-on rules live in `.github/copilot-instructions.md`.

Files:
- `coding-standards.md` — coding rules (C#, async, error handling, build hygiene)
- `testing-guidelines.md` — how to add tests (default: tests after implementation)
- `documentation-guidelines.md` — when/where/how to write docs under `docs/`
- `workflows.md` — recommended Copilot workflows + prompt library usage

Tip:
If Copilot seems to ignore a rule, reference the exact file in chat (e.g. `#docs/ai/testing-guidelines.md`)
or attach it to the chat context.