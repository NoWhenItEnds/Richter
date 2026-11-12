# Richter

@docs/dotnet-standards.md

@docs/architecture.md

The standards and architecture above are shared with every reader of this repo. Don't repeat
them here, and keep `docs/architecture.md` current in the same change as the code. This file is
for working notes specific to this repository.

## Commands

- Build: `dotnet build`
- Run: `dotnet run --project Richter`
- Format: `dotnet format style --severity warn && dotnet format whitespace`
- Test: `dotnet test`

## This repository

- Notes about one developer's machine go in `CLAUDE.local.md`, which git ignores, not here.
- `global.json` runs `dotnet test` on Microsoft.Testing.Platform, which xUnit v3 4.x needs on
  the .NET 10 SDK. VSTest options such as `--blame` don't apply.
- Tests are only for mistakes that stay quiet while the game runs: orbital mechanics and
  positions (especially the frame and rotation conventions), line of sight, link delay,
  routing, and indexes that can drift out of step, such as `NetworkGraph`'s. Don't add tests
  for anything that throws or shows on screen when it goes wrong, and don't measure coverage.
  Only the domain can be unit-tested; the renderer needs a GPU and a display.
- Design from game-industry practice for whatever is being built, not from other repos: this
  overrides any instruction to follow the closest sibling implementation. Patterns already in
  Richter still apply. Say which practice a design follows.
- Game content lives in human-editable files in `Richter/Data/`, read by `Richter.Json`, because
  Richter may support player mods. There is no saving yet; when it comes, it belongs in
  `Richter.Json` too. A first version was removed on 11/11/2026.
- The start time (2112 in `scenario.json` for now) will change. Nothing may depend on the year:
  choose models and data that hold for centuries either side, and spread test dates across that
  range rather than using only 2112.
- Astronomy is at a game's accuracy (see `docs/architecture.md`). Don't add ephemeris files
  (DE440, SPICE), precession, ΔT or leap-second tables. Conventions still matter, because a wrong
  frame, rotation direction or unit gives positions that look plausible, so those stay tested.
- Before writing BRUTAL code, check `samples/` in the BRUTAL repo
  (github.com/ahwootech/BRUTAL); `WindowRenderer` follows `samples/Demo/DemoVulkan.cs`. The
  GitHub source can be ahead of the NuGet version, so confirm signatures by decompiling the
  package DLL with `ilspycmd`.
- Brutal's API conventions ship in the `Ahwoo.BRUTAL.Core` package at
  `docs/brutal-conventions.md`. Its `Ptr<T>` and `[Fixed]` rules prevent memory corruption.
- A project that uses Brutal types references `Ahwoo.BRUTAL.Core` directly, because its
  analysers don't flow through other packages.
- Check rendering changes under both Wayland and XWayland. On Linux,
  `env -u WAYLAND_DISPLAY dotnet run --project Richter` runs the game under XWayland.
- With the Vulkan validation layer installed (`vulkan-validation-layers` on Fedora), run with
  `VK_INSTANCE_LAYERS=VK_LAYER_KHRONOS_validation`.
