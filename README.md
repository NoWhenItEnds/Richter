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

## Game data

The game reads its content at start-up from `Richter/Data/`, copied next to the executable. The
files are meant to be edited by hand. If one is missing or invalid, the game says what's wrong
and exits.

- `scenario.json`: how a game begins. `startTime` is an ISO 8601 date and time such as
  `2112-01-01T00:00:00Z`; one without an offset is UTC. `networks` places networks on bodies,
  named by their id in `solar-system.json`, or in orbit, with their devices, and `links` joins
  them by cable, radio or laser. The file's comments explain each part.
- `solar-system.json`: the Sun, planets and Moon, from published values. Its comments give the
  units and sources. Each body has an `id`, such as `earth`, that orbits and scenarios use to
  refer to it.
- `Shaders/`: the GLSL shaders that draw the map, compiled when the game starts.
- `Fonts/`: the label font, Liberation Sans, under the SIL Open Font Licence in the file beside
  it.

## Testing

```sh
dotnet test
```

## Docs

- [Architecture](docs/architecture.md)
- [.NET standards](docs/dotnet-standards.md)
