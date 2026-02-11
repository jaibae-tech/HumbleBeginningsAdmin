# MapMaker – Design Scope

This document defines what MapMaker is and is NOT.

---

## What MapMaker Is

- A deterministic world-map generator for a fantasy strategy / RPG game
- Produces a fixed, static map per seed
- Designed for offline generation via Unity editor tooling
- Modular and inspectable at every stage

---

## What MapMaker Is NOT

- NOT a climate simulation
- NOT a weather model
- NOT seasonal
- NOT time-evolving
- NOT tectonic
- NOT erosion-based
- NOT simulation-driven

All outputs are static once generated.

---

## World Assumptions

- Rainfall is static
- Biomes do not change over time
- Latitude is abstracted, not astronomical
- Elevation is noise-based, not geologic
- Rivers are algorithmic, not flow-simulated

---

## Design Guardrails

To avoid overengineering:
- If a feature requires time-based simulation → out of scope
- If a feature requires feedback loops → out of scope
- If a feature cannot be expressed as a deterministic pass → out of scope

---

## Success Criteria

MapMaker succeeds if:
- The same seed always produces the same map
- Each stage can be visually inspected
- Designers can tune outputs via ScriptableObjects
- Modules can be replaced without touching others
