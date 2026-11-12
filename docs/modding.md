# Modding Richter

Everything the game knows about its world comes from mods, including the game itself: its own
content is the mod `core`. A mod can add to anything `core` sets up, or replace parts of it,
without code. This file is the content format's specification, for modders and for us. Change it
in the same change as the format.

## A mod

A mod is a folder in `Mods/`, next to the game's executable, with a `mod.json`:

```json
{
  "id": "outer-relays",
  "name": "Outer relays",
  "version": "1.0.0",
  "contentFormat": 1,
  "dependencies": ["more-planets"]
}
```

- `id` is unique among mods. Other mods name it in their `dependencies`.
- `contentFormat` is the format the mod was written for. The game refuses a mod written for a
  different one rather than misread it. It goes up only when a change would break mods that
  already exist.
- `dependencies` (optional) lists the mods it needs. The game won't start without them.

## Load order

`core` loads first. After it, each mod loads once every mod it depends on has, and mods that
don't depend on each other load in order of id. Later mods win.

## Files

| Path in the mod | Holds | Merged by |
|---|---|---|
| `star-systems/<id>.json` | A star system's bodies: `epoch`, `referencePlaneTilt` and `bodies` | `bodies` by id |
| `scenarios/<id>.json` | A starting state: `starSystem`, `startTime`, `networks` and `links` | settings replaced; `networks` and `links` by id |
| `device-kinds.json` | The kinds of device there can be, in `kinds` | `kinds` by id |
| `shaders/*.vert`, `shaders/*.frag` | The GLSL that draws the map, compiled at start-up | replaced whole |
| `fonts/*.ttf` | The label font | replaced whole |

A mod only needs the files it changes, and each file only what it adds or changes. Where
several mods have a file at the same path, the game reads every one in load order:

- An entry in a list with ids replaces an earlier entry with the same id, in its place, or is
  added after the rest. Two entries with the same id in one file are an error.
- A setting, such as a scenario's `startTime`, replaces an earlier one. A file that leaves it out
  leaves it alone.
- Anything inside an entry belongs to it: a network's `devices` are replaced along with the
  network.
- A file that isn't data, such as a shader, comes whole from the last mod that has it.

So this `scenarios/default.json` in a mod renames `core`'s Pacific relay, keeping its links, and
adds a relay linked to London:

```json
{
  "networks": [
    { "id": "pacific-relay", "name": "Renamed relay", "surface": { "body": "earth", "latitude": 0, "longitude": 160, "altitude": 35793 } },
    { "id": "west-relay", "name": "West relay", "surface": { "body": "earth", "latitude": 0, "longitude": -20, "altitude": 35793 } }
  ],
  "links": [ { "id": "london-west", "between": ["london", "west-relay"], "medium": "radio" } ]
}
```

Each star system file's `epoch` and `referencePlaneTilt` apply to the bodies in that file, so a
mod that adds a body gives the epoch its published elements describe. `core`'s files explain
each property in their comments.

## Ids

Bodies, networks, links and device kinds have ids: short names, by convention lowercase with
hyphens, such as `earth` or `sydney-london`. They're compared exactly, so `Earth` is a different
id. Other content refers to things by them, later mods replace things by them, and
saved games will too, so once a mod is published, keep its ids. Changing one is a new thing,
not a rename.

## Device kinds and components

A device's `kind` is the id of an entry in `device-kinds.json`. A kind lists its `components`,
which are what its devices can do, with their starting settings. A device in a scenario can
change those settings for the components its kind has:

```json
// A kind, in device-kinds.json: doors start locked.
{ "id": "door", "name": "Door", "components": { "lock": { "locked": true } } }

// A device, in a scenario's network: this one doesn't.
{ "kind": "door", "name": "Airlock", "components": { "lock": { "locked": false } } }
```

| Component | Settings |
|---|---|
| `lock` | `locked`: on a kind, whether new devices start locked (left out, they don't); on a device, whether it does |

A new kind needs only data. A new component needs code. A network's gateway is a device of kind
`gateway`, named after the network, unless the network gives its own `gateway` device.

## Writing files

Files are JSON with comments and trailing commas allowed. A property the game doesn't know is
an error, because it's usually a typo. Times are ISO 8601, such as `2112-01-01T00:00:00Z`, and
one without an offset is UTC. Distances are in km, angles in degrees and periods in days. If
something is wrong, the game names the file and the entry and doesn't start.

## Not yet

These will come without changing anything above:

- Removing an entry, or changing one property of another mod's entry without replacing it.
- Scripts or code in mods.
- Choosing which mods load, and in what order, in the game, and a mods folder in the player's
  own files.
- Choosing a scenario: the game starts `default`.
