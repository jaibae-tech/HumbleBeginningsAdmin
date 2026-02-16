# Module 1 Notes — Elevation

Module 1 generates the **physical substrate** of the world: a continuous elevation field plus several derivative fields that later modules use to place climate, water, biomes, threats, landmarks, roads, and rendering cues.

The key design goal is **macro-first realism** with **scale-aware controls**:
- The same config works across multiple map sizes because feature sizes are expressed in **miles** (or densities per million square miles), and converted to counts/scales per run.
- Coastlines are produced via **sea-level percentile** (not hard edge clamping), then refined with **fade/carve/shelf**.
- Lakes are intentionally **not** produced in Module 1; inland water is deferred to Hydrology. Module 1 can embed **basins** (negative relief) to make later lakes/rivers believable.

## Outputs written into WorldArrays

Primary elevation layers:
- `ElevationRaw` — continuous float elevation field.
- `ElevationBands` — final discrete bands for preview and downstream classification.

Macro scaffolding (debug/inspection + downstream hints):
- `LandMask01` — 0..1 land probability/strength field.
- `PlateId` — Voronoi plate partition.
- `Uplift01` — 0..1 uplift driver from plate boundaries.
- `Ruggedness01` — 0..1 ruggedness proxy (updated after conditioning/micro-relief).

Coast/water flags (Module 1 only):
- `IsOcean`, `IsDeepOcean`, `IsCoastalShelf` — ocean classification.
- `IsInlandLake` — **not used** after Step 4; inland seas are removed so this should end empty.

Derivative fields (for later modules / rendering):
- `Slope01` — 0..1 local gradient proxy.
- `Aspect01` — 0..1 downhill direction (0=east, 0.25=north, 0.5=west, 0.75=south).
- `Curvature01` — 0..1 signed curvature proxy (0.5 flat; <0.5 valley/concave; >0.5 ridge/convex).
- `CoastDistance01` — 0..1 distance-to-coast (0 at coastline; 1 far inland).

## Preview exports (debug PNGs)

Module 1 writes several diagnostic images to support tuning and validation:
- `WorldPreview_00_Elevation_Grayscale.png`
- `WorldPreview_01_ElevationBands.png`
- `WorldPreview_01_LandMask.png`
- `WorldPreview_01_Plates.png`
- `WorldPreview_01_Uplift.png`
- `WorldPreview_ShadedRelief.png`
- `WorldPreview_Topographic.png`
- `WorldPreview_Slope.png`
- `WorldPreview_CoastDistance.png`
- `WorldPreview_Aspect.png`
- `WorldPreview_Curvature.png`

## Practical sequencing model (what each step is responsible for)

1. **Macro scaffolding**
   - Builds coarse crust field and land mask.
   - Seeds tectonic plates and derives uplift along boundaries.

2. **Coastline resolution**
   - Applies coast fade and carve fields.
   - Shapes continental shelf.
   - Ocean is chosen by **percentile** (`OceanPercent`).

3. **Field conditioning**
   - Soft-clamps extremes.
   - Remaps to a stable range.
   - Applies **very low-strength** smoothing at a large radius (macro-safe).

4. **Ocean connectivity**
   - Ensures all ocean tiles are connected to the map edge.
   - Lifts inland seas above sea-level (no inland water after Module 1).

5. **Relief coherence**
   - Spreads uplift influence into surrounding terrain to create **foothills**.
   - Reduces harsh adjacency (mountain directly adjacent to lowland).

6. **Basin embedding**
   - Adds gentle depressions (basins) and rims.
   - No water classification—these are **hydrology scaffolds**.

7. **Micro-relief**
   - Adds small-scale variation without speckling.
   - Updates `Ruggedness01`.

8. **Final preparation**
   - Final normalization.
   - Computes derivatives (slope/aspect/curvature/coast distance).
   - Performs stability checks and exports.

## Tuning workflow

1. Change only **miles-based knobs** and **densities** in `HB_ElevationConfig`.
2. Run and inspect:
   - Elevation Grayscale, Shaded Relief
   - LandMask, CoastDistance
   - Bands/Topographic for category distribution
3. If the **continent outline changes unexpectedly**, the conditioning/micro-relief is too strong.
4. If **coast distance is black**, ocean seeding is missing (no ocean tiles), or the coast-distance pass ran before band/ocean classification.

