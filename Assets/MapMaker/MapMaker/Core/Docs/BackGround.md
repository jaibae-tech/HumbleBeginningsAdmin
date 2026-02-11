WORLD SEEDER NOTES 1 (EXPANDED)
=================================

AUTHORITATIVE CONSOLIDATION DOCUMENT
-----------------------------------
This document is intended to be migrated into a NEW CHAT and merged with
other design references to produce a SINGLE canonical design document.

It contains ALL assumptions, constraints, invariants, and examples needed
to correctly interpret the World Generator / Seeder system.

If a rule is not contradicted by a later document, THIS DOCUMENT SHOULD
BE CONSIDERED AUTHORITATIVE FOR INITIAL WORLD BUILD.

==========================================================================
1. ROLE OF THE WORLD SEEDER
==========================================================================

The World Seeder is responsible for creating the *initial state of the world*.
This happens exactly once per world instance.

The output of the World Seeder becomes the immutable baseline consumed by:
- The World Engine (weekly updates)
- The Mission System
- The Caravan System
- The Item & Knowledge Systems

The World Seeder MUST NOT:
- Simulate time
- Generate missions
- Track players
- Modify the world after creation
- Perform balance adjustments

Think of the World Seeder as **world birth**, not world simulation.

==========================================================================
2. DETERMINISM & SEED CONTROL
==========================================================================

A single immutable WORLD SEED drives all generation.

The seed deterministically controls:
- Map shape and terrain
- Elevation gradients
- River paths
- Region boundaries
- Anchor placement (surface + underground)
- Underground topology
- Initial hierarchies
- Initial POI placement
- Initial threat, danger, and influence values

NO SECONDARY RNG IS PERMITTED.

If two worlds are generated with the same seed and configuration,
THEY MUST BE IDENTICAL.

Example:
Seed = 184726
Config = { map_size: 300x300, tile_size: 2 miles }
→ The resulting world MUST always match bit-for-bit.

There will be declared later in modules:

Max Region Size
Min Region Size
additional tunable fields as needed per module.  Any global tunable will be at the core/config level and shared.


==========================================================================
3. WORLD MAP & TILE SYSTEM
==========================================================================

3.1 Grid Definition
------------------
- Hex-based grid
- Tile size: 2 miles per tile (configurable)
- x/y  tile number configurable

Tiles are the smallest spatial unit used by:
- Terrain
- Movement cost
- POI placement
- Region membership

==========================================================================
4. TERRAIN & ELEVATION GENERATION
==========================================================================

4.1 Terrain Types
-----------------
Supported terrain tiles:

TBD in Features Pass

4.2 Elevation Model
-------------------
Elevation is:
- Seed-derived
- Static
- Independent from later world updates

Elevation influences:
- Terrain transitions (plains → hills → mountains)
- River origin and flow
- Initial danger weighting
- Movement cost

NO climate, erosion, or tectonic changes occur post-generation.

==========================================================================
5. REGION GENERATION
==========================================================================

5.1 Region Definition
---------------------
Regions are contiguous clusters of tiles sharing:
- Dominant terrain type
- Similar elevation band by family (forest, mountains, mountains_hills (mountain chain), plains, swamp, jungle, desert, etc)
- Regions absorb lakes - lakes do not form own regions.

Regions are NOT political entities.

5.2 Region Size Rules
---------------------
- A region may not exceed 10% of total world tiles (configurable)
- Oversized terrain clusters MUST be split

Example:
If a mountain chain spans 25% of the map,
it must be split into multiple mountain regions.

5.3 Region Initialization
-------------------------
Each region initializes with:

- Terrain type
- Starting danger
- Starting threat
- Minimum danger 
- Maximum danger cap
- minimum threat 
- Maximum threat cap
- Active flag = false

Starting danger/threat derived from:
- Distance from positive surface anchors
- Proximity to hostile eastern border
- Terrain hostility

==========================================================================
6. RIVERS & NATURAL FEATURES
==========================================================================

Rivers:
- Originate in Mountains or High Mountains
- Flow downhill only
- Terminate in oceans or lakes
- Must form continuous paths
- Cannot split unnaturally

Rivers influence:
- Settlement placement
- Region boundaries
- Logical road placement later

==========================================================================
7. SURFACE ANCHORS (STATIC WORLD STRUCTURES)
==========================================================================

7.1 Anchor Categories
--------------------
Surface anchors are permanent map structures.

Types:
- Positive Anchors (permanent, unkillable)
- Hostile Anchors (permanent, unkillable)

Rules:
- Anchors are region-bound
- Maximum ONE anchor per region

7.2 Positive Anchor Placement
-----------------------------

Placed FIRST.

Positive anchors define:
- Starting safety gradient
- Initial travel routes
- Knowledge repositories

Required starting positive anchors (starting side BIAS):

- 3 starting positive anchors.

Example:
- Elf city → largest forest
- Dwarf city → largest mountain range
- Gnome settlement → forest/mountain/river convergence
- Halfling town → hills/plains convergence
- Human (coastal) → coast near river
- Human (nomads) → largest plains
- Human (desert-edge) → desert border
- Human (north) → tundra edge

Example:
The Dwarven city is placed in the deepest mountain region on starting side of the map,
NOT simply any mountain tile.

==========================================================================
8. HOSTILE SURFACE ANCHORS
==========================================================================

Placed AFTER positive anchors.

Rules:
- Biased toward the opposite side of the map from player anchors
- Terrain-appropriate
- Maximum ONE hostile anchor per region (only 3-5 per map)


Hostile anchors:
- Seed hostile hierarchies
- Define threat escalation zones
- Can have their influence lowered by missions and tile loss

==========================================================================
9. UNDERGROUND WORLD (INITIAL GENERATION)
==========================================================================

The underground is NOT a second map.

It is:
- Seed-generated
- Graph-based
- Anchor-driven

It has:
- No tiles
- No regions
- No influence spread
- No free exploration

==========================================================================
10. UNDERGROUND ANCHORS
==========================================================================

Examples:
- Stonehold (Dwarven deep city)
- Ikillu (Drow city)
- Abyssal nodes

Rules:
- Permanent and unkillable
- No surface entrances
- Not destinations
- Accessed ONLY through missions
- Used only for threat/danger calculations and mission context

ITEM RULE:
Items lost underground are PERMANENTLY DESTROYED.
They are NOT tracked or recoverable.

==========================================================================
11. UNDERGROUND ENTRANCES
==========================================================================

- Each surface region has exactly ONE underground entrance
- Entrance is seed-derived
- Entrance is a POI (cave, chasm, ruin shaft)

Access is ONLY via missions.
Players cannot click and travel underground.

==========================================================================
12. HIERARCHY SEEDING
==========================================================================

For each anchor:

- Create ONE dominant hierarchy
- Assign:
  - Leader (highest difficulty)
  - Lieutenants
  - Subordinates

Hierarchy strength is scaled by:
- Region danger
- Region threat

Hierarchies start UNDISCOVERED.

==========================================================================
13. EPHEMERAL STRUCTURES & LAIRS
==========================================================================

Ephemeral POIs include:
- Camps
- Lairs
- Temporary strongholds

Rules:
- Spawned based on initial threat
- One POI per tile maximum
- Not permanent
- Start undiscovered

==========================================================================
14. STARTING EPHEMERAL OBJECTS
==========================================================================

- Placed near hostile anchors
- Difficulty scaled to region danger
- Undiscovered at world start

Example:
An Orc stronghold region will seed:
- Orc camps
- Nearby dungeons
- Patrol lairs

==========================================================================
15. POI RULES (GLOBAL)
==========================================================================

- One POI per tile maximum (does not include presence of a landmark)
- All POIs start undiscovered
- Discovery is player-specific until shared
- POIs may be removed later by:
  - Destruction
  - Supersession

==========================================================================
16. THREAT, DANGER, INFLUENCE INITIALIZATION
==========================================================================

Danger:
- Represents encounter difficulty (monster/beast/humanoid level allowed)
- Derived from:
  - Distance to positive anchors
  - Terrain hostility
  - Distance to negative anchors

Threat:
- Represents density and escalation potential
- Derived from:
  - Proximity to hostile anchors
  - Terrain capacity

Influence:
- Exerted by anchors
- Modifies danger
- Respects danger caps
- Modifies encounter bestiary

==========================================================================
17. INITIAL WORLD GUARANTEES
==========================================================================

The generated world MUST guarantee:

1. At least one low-danger region
2. Logical terrain transitions
3. No overlapping anchors
4. Valid travel paths
5. Clear hostile escalation gradient
6. Full determinism

==========================================================================
18. FINAL INVARIANT
==========================================================================

IF THE SAME SEED IS USED,
THE SAME WORLD MUST BE PRODUCED.

NO EXCEPTIONS.

==========================================================================
END OF DOCUMENT
==========================================================================
