# Richter

A standalone game built on [BRUTAL](https://github.com/ahwootech/BRUTAL), C# bindings for GLFW
and Vulkan.

## Requirements

- The .NET 10 SDK.
- A GPU and driver with Vulkan 1.3. Windows GPU drivers include one. On Linux, install the
  Vulkan loader and a driver (`vulkan-loader` and `mesa-vulkan-drivers` on Fedora). On macOS,
  install the Vulkan SDK, which provides MoltenVK.
- A desktop session: Windows, macOS on Apple silicon, or Linux with Wayland or X11.

## Running

```sh
dotnet run --project Richter
```

## Controls

| Key | Does |
|---|---|
| Tab, Shift+Tab | Centre the map on the next or previous body |
| Scroll wheel | Zoom |
| Space | Pause or resume time |
| `+`, `-` | Run time faster or slower, from ×1 to ×1,000,000 |

## Game data and mods

All of the game's content comes from mods in `Mods/`, next to the executable, and its own
content is the mod `core`, in `Richter/Mods/core/`. Another mod can add to it or replace parts of
it without code; [modding.md](docs/modding.md) explains how. The files are meant to be edited by
hand. If one is missing or invalid, the game says which file and entry, and exits.

`core` holds:

- `scenarios/default.json`: how a game begins: its star system, its start time, and the
  networks, devices and links in place. The file's comments explain each part.
- `star-systems/solar-system.json`: the Sun, planets and Moon, from published values. Its
  comments give the units and sources.
- `device-kinds.json`: the kinds of device there can be, such as doors, and what they can do.
- `shaders/`: the GLSL shaders that draw the map, compiled when the game starts.
- `fonts/`: the label font, Liberation Sans, under the SIL Open Font Licence in the file beside
  it.

## Testing

```sh
dotnet test
```

## Committing

A pre-commit hook runs the checks from the [.NET standards](docs/dotnet-standards.md): a build
with warnings as errors, the style and whitespace checks, and the tests. Turn it on once in each
clone:

```sh
git config core.hooksPath .githooks
```

## Docs

- [Architecture](docs/architecture.md)
- [Modding](docs/modding.md)
- [.NET standards](docs/dotnet-standards.md)
