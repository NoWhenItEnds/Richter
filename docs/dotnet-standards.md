# .NET standards

How we write C#. It applies to every .NET repo that carries a copy of this file, and is read by
people and by Claude Code (each repo's `CLAUDE.md` imports it). Most code is written by AI and
reviewed by people, so where a rule could go either way, it goes the way that makes review
faster.

Formatting and mechanical style live in `.editorconfig`, and `Directory.Build.props` reports
them during `dotnet build`. This file covers what a tool can't check. All three files are
identical in every repo that uses them, so change them everywhere rather than in one repo's
copy. A repo's own `.editorconfig` rules go at the bottom of its copy under their own heading,
and its own working notes go in its `CLAUDE.md`.

## Before a change is done

- `dotnet build` adds no new warnings.
- `dotnet test` passes, in repos that have tests.
- `dotnet format style --severity warn && dotnet format whitespace` has been run, in that
  order, because the style fixes change indentation. Together they fix whitespace, using order,
  braces, `var`, keyword type names, namespaces and the rest of `.editorconfig`. Plain
  `dotnet format` also applies third-party analyser fixes, so use the two subcommands. Add
  `--verify-no-changes` to either one to check without changing anything.
- Docs that the change makes wrong are updated in the same change: `docs/architecture.md`, the
  README, and any plan or known-issues entry it affects.

## Language

- Australian spelling wherever we choose the words: comments, XML docs, log and error
  messages, READMEs, commit messages, and our own identifiers (`ClearColour`,
  `InitialiseAsync`, `SerialiseState`).
- Names that come from a framework or library keep their spelling, because we can't change
  them: `Color`, `JsonSerializer`, `[Serializable]`, and members we override or implement.
- Rename one of our own identifiers that uses US spelling in a change of its own. A name that
  is saved or sent somewhere (save files, JSON properties, network messages) is part of a
  format, so renaming it breaks existing data.
- Dates in prose are DD/MM/YYYY.

## Solution layout

Projects are named `<Product>.<Part>`.

| Project | Holds |
|---|---|
| `<Product>` | The executable: `Program.cs`, start-up and wiring |
| `<Product>.Domain` | State, rules and logic that need no infrastructure |
| `<Product>.<Technology>` | One project per external system it wraps, such as a renderer or a database, named after its main technology: `.Vulkan`, `.Sqlite`. Libraries that only serve that system go in the same project. |
| `<Product>.Tests` | The unit tests |

References point inward. The executable references the other projects, technology projects
may reference `.Domain`, and `.Domain` references no other project. Put new code in an existing
project or folder before creating a new one, and record a new one in `docs/architecture.md`.

## Architecture doc

Every repo has `docs/architecture.md`. It is the first thing a reviewer or Claude reads:
`CLAUDE.md` imports it and the README links to it. In about 100 lines it covers:

- what the repo owns, and what it only mirrors from elsewhere;
- a Mermaid diagram of the components and the systems around them;
- the main flows traced end to end, naming the classes involved: for a service, one read and
  one write; for an application, start-up and one frame or user action;
- background work: events, schedules and threads;
- where the rules and invariants are enforced;
- the traps: things that look safe to change and aren't.

Update it in the same change as anything that makes it wrong.

## Control flow

- A method may start with guard clauses: checks that `return` or `throw` when an argument, or a
  lookup the rest of the method depends on, rules out doing the work. Guards come first, before
  the method changes anything.
- After the guards, one exit. Declare the result, branch with `if`/`else if`/`else`, and
  `return` once at the end. No `return` from the middle of a method, no `continue`, and `break`
  only in `switch`.

```csharp
public String Describe(Item? item)
{
    if (item == null)
    {
        return "Nothing";
    }

    String description;

    if (item.Quantity == 1)
    {
        description = item.Name;
    }
    else
    {
        description = $"{item.Name} x{item.Quantity}";
    }

    return description;
}
```

## Types and members

`.editorconfig` already enforces these: CLR type names (`String`, `Int32`, `Boolean`),
explicit types instead of `var`, file-scoped namespaces, braces on every block, no `this.`,
explicit constructors (no primary constructors), and block-bodied methods.

Beyond those:

- Leave classes unsealed unless there is a reason to seal one, as the Framework Design
  Guidelines advise. Existing sealed classes don't need unsealing.
- Use records for results, DTOs and other immutable values.
- Give enum members explicit values (`Idle = 0`), because they end up in saved data and
  messages.
- Depend on concrete classes. Add an interface only when there is more than one implementation
  or a real boundary (between projects, or around an external system). Don't add one just to
  make a type mockable.
- Keep helpers `internal` and grant the test project access with `InternalsVisibleTo` rather
  than making them `public` for tests.
- Use `DateTimeOffset` for instants, never `DateTime`.
- Fail at start-up when required configuration is missing, not on first use.

## Naming

- Non-public fields follow the SDK defaults:
  - instance fields, `readonly` or not, are `_camelCase`: `_window`, `_frameCount`.
  - mutable static fields are `s_camelCase`: `s_instanceCount`.
  - `static readonly` fields and constants are PascalCase: `MaxFramesInFlight`,
    `DefaultTimeout`.
- The format check catches the `_` and `s_` prefixes. It can't catch an UPPER_SNAKE constant or
  `static readonly` field, because the analyser's PascalCase check only looks at the first
  letter, so review has to.
- `dotnet format` can't rename fields. Use the IDE's fix for IDE1006, which renames every
  reference.
- `Async` suffix on every async method you name. Names the runtime or an interface dictates
  (`Main`, interface members) keep that name.
- `CancellationToken cancellationToken` is the last parameter of every async method and is
  passed to every async call inside it.

## Errors and logging

- Never catch and ignore. Catch the specific exception you can handle. A `catch` that
  deliberately does nothing says why in a comment.
- Log with message templates and named placeholders (`"Loaded {Count} assets", count`), never
  interpolated strings, so the values stay structured.
- Errors shown to users don't leak internal detail (stack traces, file paths, secrets), and
  nothing logs a secret.

## Documentation comments

Write a doc comment when it tells the reader something the code doesn't. Every one has to be
read and checked in review, and a wrong one is worse than none.

- Every type gets a `<summary>` saying what it is for and where it fits.
- Document a member when its name and signature don't tell the whole story. Always document
  public API that another project or package consumes.
- Don't restate the code: "Initialises the class with its dependencies", a `<param>` that
  repeats the parameter's name, "must not be null" on a non-nullable type, or a private field
  whose name already says what it holds. Existing ones can stay; don't add more.
- Say what the signature can't: what `null` means ("null until the first frame is drawn"),
  units, time zones, threading, side effects, and who owns or disposes a value.
- Put the reasoning (ordering, trade-offs, why the obvious approach is wrong) in `<remarks>`.
  These are the most useful comments in the codebase; keep them.
- Short ones go on one line with a space inside the tags:
  `/// <summary> Holds the state of one save slot. </summary>`. Longer ones wrap at about 100
  characters. Use `<see cref>` for types and members, `<c>` for literals, and `<inheritdoc/>`
  on overrides and interface implementations.
- Inline comments explain why, never what.

The compiler's missing-doc warning (CS1591) is off in `Directory.Build.props`, because it would
demand the boilerplate above on every public member; review checks this rule instead.

## Tests

xUnit v3, one `<Product>.Tests` project per solution.

- Pure, in-process logic only. Nothing reaches a database, the network, the GPU, a display or a
  container. Anything that needs those is an integration test and doesn't belong in this suite.
- No mocking framework and no assertion library. Use the real framework type (a
  `MemoryStream`, an in-memory `IConfiguration`), hand-write a small stub at the boundary, or
  extract the pure part into an `internal` method and test that.
- One file per production type, `<Type>Tests.cs`, in a folder that mirrors the type's folder.
- Name the behaviour, not the method: `FallsBackToTheDefaultWhenTheSaveFileIsMissing`, not
  `Load_Test3`.
- `[Theory]` for tables of cases instead of near-duplicate `[Fact]`s.
- No test reads the clock; every instant is a literal.
- Keep environment-variable changes inside a single test class, because xUnit runs classes in
  parallel.
- When a test pins behaviour that is surprising rather than wanted, say so in a comment.
- Never raise coverage by excluding files from it.

## Dependencies and build

- `net10.0`, with `Nullable` and `ImplicitUsings` enabled in every project.
- Ask before adding a NuGet package. We don't use mapping, mediator, validation, mocking or
  assertion libraries; write the plain code.
- Commit `packages.lock.json` with every package change. `Directory.Build.props` turns lock
  files on so every restore resolves the same versions, transitive ones included.

## Git

- Conventional Commits with a lowercase type: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`,
  `chore:`.
- Branches are `<type>/<short-description>`, e.g. `fix/save-file-loading`.
- A formatting-only change or a rename goes in its own commit, so it doesn't hide the real
  change in review.
