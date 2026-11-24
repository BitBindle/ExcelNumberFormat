# AGENTS.md

These rules apply to each agent that changes this repository.
If a rule in this file differs from another document, this file applies.

## Repository

ExcelNumberFormat is a .NET library.
It parses ECMA-376 number format strings and formats values like Excel.

| Path | Content |
| --- | --- |
| `src/ExcelNumberFormat` | The library. It targets `net20`, `netstandard1.0` and `netstandard2.0`. |
| `src/Example` | A console app that shows the library. It targets `net10.0`. |
| `test/ExcelNumberFormat.Tests` | NUnit tests. They target `net10.0`. |

The base branch is `master`.

## Build and test

Do these steps in this order.

1. Build with `dotnet build ExcelNumberFormat.sln -c Release -p:TreatWarningsAsErrors=true`.
2. Test with `dotnet test test/ExcelNumberFormat.Tests/ExcelNumberFormat.Tests.csproj -c Release --no-build`.
3. If a step fails, fix the cause.

Do not skip, disable or ignore a test to make a step pass.
Do not weaken an expected value to make a test pass.

The CI workflow `.github/workflows/ci.yml` runs the same two commands.

## Review gate

Do this gate before you give a change to a person.

1. Finish the change, the build and the tests.
2. Save the diff against `master` to a file outside the repository.
3. Start a new read-only reviewer. In Claude Code, use a subagent.
4. Give the reviewer the task, the path of the diff file and the results of the build and tests.
5. Do not give the reviewer your own assessment of the change.
6. The reviewer returns one verdict: **BLOCK**, **APPROVE WITH NOTES** or **APPROVE**.
7. If the verdict is **BLOCK**, fix each objection. Then run the gate again with a new reviewer.
8. If the verdict is **APPROVE WITH NOTES**, fix the notes that you can. Tell the person about the other notes.

Do not give a person a change that has a **BLOCK** verdict.
Do not replace a fix with a caveat.

The reviewer gives **BLOCK** for one of these reasons:

- A correctness bug.
- A security problem.
- A failed build or test.
- A new code path that no test covers.
- A broken rule in this file.

## Problems that the change did not cause

1. If the fix is small and its effect is clear, fix it in the same change. Tell the reviewer and the person.
2. If the fix is not small, log a separate GitHub issue. Give the problem, its location and the steps to reproduce.
3. Do not fix a large problem in the same change.

## Code rules

### Compatibility

- The library targets `net20` and `netstandard1.0`. Use only APIs that exist in all target frameworks.
- If a newer API is necessary, put it behind a conditional compilation symbol, for example `#if NETSTANDARD1_0`. Keep the same behavior on each target.
- Do not add a package reference to the library. The library has no dependencies.
- Keep the public API compatible. Change it only if the task asks for the change.
- Document each public member with an XML comment. The build generates the documentation file.

### Behavior

- The reference is Excel. A test states the Excel result that the code must give.
- Give the source of an expected value in a comment. For example, the Excel version or the test file of another project.
- Format with the culture that the caller gives. Do not add a new read of `CultureInfo.CurrentCulture`. The only read is in `Parser.cs`, for the default time pattern.
- Do not throw an exception for an invalid format string. Set `NumberFormat.IsValid` to false.

### Tests

- Each new code path has a test. This includes each new branch.
- Put tests in `test/ExcelNumberFormat.Tests`. Use NUnit.
- Name a test file for the feature that it tests.
- Do not put an issue number in a test name.

### Memory

- Use `Span<T>`, `stackalloc` and `ArrayPool<T>` only where the target frameworks have them and where they remove an allocation on a hot path.
- Do not use them where they only make the code harder to read.

### Unnecessary work

- Do not add an extra copy or an extra pass over the input.
- Do not read data that the request does not use.

## Language and documentation

Use ASD-STE-100 (Simplified Technical English) everywhere.
This includes replies, code, comments, documentation, commit messages and pull requests.
Identifiers, test names and headings follow the same rules.

- Write one idea in each sentence.
- Write instructions with 20 words or fewer. Write descriptions with 25 words or fewer.
- Write paragraphs with six sentences or fewer.
- Use active voice and present tense.
- Use one word for one meaning. Use plain, common words.
- Do not use metaphors, idioms, marketing adjectives or emoji.
- Do not assess your own work with words such as "comprehensive", "robust" or "seamless".
- Delete each sentence that you can remove without a loss.
- Write a procedure as numbered steps. Put the condition before the action.
- Give the reason or the specification requirement in a comment or test title. Do not give a task number.
