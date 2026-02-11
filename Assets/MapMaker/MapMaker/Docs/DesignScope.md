# DesignScope

MapMaker generates a deterministic, fixed overworld map for a fantasy game. It is a build-time/editor tool.

## What MapMaker is
- A deterministic pipeline: same seed + same configs -> same arrays -> same exports.
- A modular system: each module is isolated (Config + Scripts + Docs) and called by the thin driver.
- An inspection-first tool: each module can export a focused PNG for debugging, and there is a stacked PNG that layers all non-latitude outputs.

## What MapMaker is not
- Not a full climate simulator: no global circulation, no dynamic weather.
- Not a seasonal simulation: no changing seasons, no snowpack accumulation over time.
- Not a world-state simulator: the generated map is considered static once created.

## Core gameplay constraints (assumptions)
- **Fixed map**: geography does not change during play.
- **Simple moisture/rain**: rainfall is simplified; moisture is derived and static.
- **Latitude is informational**: latitude influences biomes but is not drawn in the stacked debug PNG.

## Outputs
- Runtime arrays (WorldArrays) and optional instance lists (landmarks/anchors).
- Per-module PNG previews.
- A stacked PNG preview that includes all modules except latitude.
- Optional JSON export later (not required early).

## Non-goals / avoid overengineering
- Avoid tectonics plates, erosion simulation, or heavy CFD.
- Prefer quantile/threshold approaches and lightweight distance fields.
- Keep validation "soft" by default: warn + clamp rather than hard-fail, except for structural impossibilities.
