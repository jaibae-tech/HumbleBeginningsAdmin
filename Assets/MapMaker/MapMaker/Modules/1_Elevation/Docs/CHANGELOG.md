# Changelog — Module 1 (Elevation)

## 2026-02-14

- Complete rewrite to a **scale-aware, macro-first** elevation model.
- Consolidated configuration into a small set of **miles-based** knobs with derived counts/scales.
- Added deterministic debug logging of derived values to support tuning from logs.

## Module 1 plan (steps 1–8)

1. **Macro scaffolding** — create initial land mask, plates, and uplift drivers.
2. **Coastline resolution** — sea-level via percentile + coast fade/carve + shelf shaping.
3. **Field conditioning** — clamp/remap + very-low-strength macro-safe smoothing.
4. **Ocean connectivity** — remove inland seas (no inland water after Module 1).
5. **Relief coherence** — reduce harsh adjacency (foothills / regional relief spread).
6. **Basin embedding** — depressions + rims as hydrology scaffolding (no water classification).
7. **Micro-relief** — add local variation without corrupting macro geography.
8. **Final preparation** — stability checks + derivative fields + exports for downstream modules.
