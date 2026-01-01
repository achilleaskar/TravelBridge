# Testing guidelines

This doc extends the repo’s general testing rules with a practical workflow.

## Default workflow (unless you explicitly ask for TDD)
1) Implement the change
2) Ensure `dotnet build` is green
3) Ensure existing tests pass (`dotnet test`)
4) Add/extend tests for the new behavior
5) Run tests again and fix failures

## MySQL considerations
- Prefer **unit tests** for services/validators/mappers (fast and stable).
- Add **integration tests** only if the repo already has a pattern for them.
- Do **not** introduce new infrastructure (Docker/Testcontainers/real DB) unless the repo already uses it or you approve it.
- For DB-related bugfixes, add a regression test at the highest level that is feasible with existing tooling.

---

## Test structure

- Separate test project: **`[ProjectName].Tests`**.
- Mirror classes: `CatDoor` -> `CatDoorTests`.
- Name tests by behavior: `WhenCatMeowsThenCatDoorOpens`.
- Follow existing naming conventions.
- Use **public instance** classes; avoid **static** fields.
- No branching/conditionals inside tests.

## Unit Tests

- One behavior per test;
- Avoid Unicode symbols.
- Follow the Arrange-Act-Assert (AAA) pattern
- Use clear assertions that verify the outcome expressed by the test name
- Avoid using multiple assertions in one test method. In this case, prefer multiple tests.
- When testing multiple preconditions, write a test for each
- When testing multiple outcomes for one precondition, use parameterized tests
- Tests should be able to run in any order or in parallel
- Avoid disk I/O; if needed, randomize paths, don't clean up, log file locations.
- Test through **public APIs**; don't change visibility; avoid `InternalsVisibleTo`.
- Require tests for new/changed **public APIs**.
- Assert specific values and edge cases, not vague outcomes.

## Test workflow

### Run Test Command

- Look for custom targets/scripts: `Directory.Build.targets`, `test.ps1/.cmd/.sh`
- .NET Framework: May use `vstest.console.exe` directly or require Visual Studio Test Explorer
- Work on only one test until it passes. Then run other tests to ensure nothing has been broken.

### Code coverage (dotnet-coverage)

- **Tool (one-time):**
  bash
  `dotnet tool install -g dotnet-coverage`
- **Run locally (every time add/modify tests):**
  bash
  `dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test`

## Test framework-specific guidance

- **Use the framework already in the solution** (xUnit/NUnit/MSTest) for new tests.

### xUnit

- Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`
- No class attribute; use `[Fact]`
- Parameterized tests: `[Theory]` with `[InlineData]`
- Setup/teardown: constructor and `IDisposable`

### xUnit v3

- Packages: `xunit.v3`, `xunit.runner.visualstudio` 3.x, `Microsoft.NET.Test.Sdk`
- `ITestOutputHelper` and `[Theory]` are in `Xunit`

### NUnit

- Packages: `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`
- Class `[TestFixture]`, test `[Test]`
- Parameterized tests: **use `[TestCase]`**

### MSTest

- Class `[TestClass]`, test `[TestMethod]`
- Setup/teardown: `[TestInitialize]`, `[TestCleanup]`
- Parameterized tests: **use `[TestMethod]` + `[DataRow]`**

### Assertions

- If **FluentAssertions/AwesomeAssertions** are already used, prefer them.
- Otherwise, use the framework’s asserts.
- Use `Throws/ThrowsAsync` (or MSTest `Assert.ThrowsException`) for exceptions.

## Mocking

- Avoid mocks/Fakes if possible
- External dependencies can be mocked. Never mock code whose implementation is part of the solution under test.
- Try to verify that the outputs (e.g. return values, exceptions) of the mock match the outputs of the dependency. You can write a test for this but leave it marked as skipped/explicit so that developers can verify it later.