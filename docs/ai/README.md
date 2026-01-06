# AI Customization Documentation

This folder contains **detailed** guidance for Copilot and AI assistants working with TravelBridge.

The repo-wide always-on rules live in `.github/copilot-instructions.md`.

## Files

| File | Description |
|------|-------------|
| `quick-context.md` | Quick-load reference - file map, conventions, flows |
| `coding-standards.md` | C# coding rules, async, error handling, build hygiene |
| `testing-guidelines.md` | How to add tests (default: tests after implementation) |
| `documentation-guidelines.md` | When/where/how to write docs under `docs/` |
| `workflows.md` | Recommended Copilot workflows + prompt library usage |

## Usage Tips

1. **Reference files in chat**: If Copilot seems to ignore a rule, reference the exact file:
   ```
   #docs/ai/testing-guidelines.md
   ```

2. **Quick context first**: Start with `quick-context.md` for fast codebase understanding

3. **Check parent docs**: Full technical documentation is in `docs/` (parent folder)