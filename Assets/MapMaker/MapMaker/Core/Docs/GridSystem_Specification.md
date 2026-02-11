# Grid System Specification: Hexagonal Tiles

**Version:** 1.0  
**Grid Type:** Hexagonal (6 neighbors per tile)  
**Coordinate System:** Offset coordinates (odd-r)

---

## Overview

This document defines the hexagonal grid system used throughout world generation and gameplay. All modules MUST use the GridSystem utilities for neighbor calculations.

---

## Hex Layout

### Visual Representation

```
     [NW]  [NE]
       \   /
   [W]--[X]--[E]
       /   \
     [SW]  [SE]
```

### Properties
- **Neighbors per tile:** 6
- **Directions:** E, SE, SW, W, NW, NE (clockwise from East)
- **Equidistant:** All neighbors are same distance from center
- **No diagonal problem:** Unlike square grids, all movement costs are equal

---

## Coordinate System: Offset (odd-r)

### Why Offset Coordinates?

- **Simple storage:** Standard 2D array (y * width + x)
- **Easy visualization:** Rectangular texture maps work
- **Compatible with existing code:** Minimal changes from square grid

### Layout Rules

**Even rows (y % 2 == 0):** Tiles at positions x=0, 1, 2, 3...
**Odd rows (y % 2 == 1):** Tiles offset by +0.5 in X

```
Visual layout (top view):
Row 0 (even): [ 0 ] [ 1 ] [ 2 ] [ 3 ]
Row 1 (odd):    [ 0 ] [ 1 ] [ 2 ] [ 3 ]
Row 2 (even): [ 0 ] [ 1 ] [ 2 ] [ 3 ]
Row 3 (odd):    [ 0 ] [ 1 ] [ 2 ] [ 3 ]
```

### Array Storage

```csharp
// Linear array storage
TileDesc[] tiles = new TileDesc[width * height];

// Access tile at (x, y)
int index = y * width + x;
TileDesc tile = tiles[index];
```

---

## Direction Encoding

### Direction Enum

```csharp
public enum HexDirection : byte
{
    E  = 0,  // East
    SE = 1,  // Southeast
    SW = 2,  // Southwest
    W  = 3,  // West
    NW = 4,  // Northwest
    NE = 5   // Northeast
}
```

### Direction Properties

| Direction | Angle | Opposite |
|-----------|-------|----------|
| E (0)     | 0°    | W (3)    |
| SE (1)    | 60°   | NW (4)   |
| SW (2)    | 120°  | NE (5)   |
| W (3)     | 180°  | E (0)    |
| NW (4)    | 240°  | SE (1)   |
| NE (5)    | 300°  | SW (2)   |

---

## Neighbor Calculation

### Algorithm

Neighbor coordinates depend on whether current row is even or odd:

**Even rows (y % 2 == 0):**
```
E:  (x+1, y)
SE: (x,   y+1)
SW: (x-1, y+1)
W:  (x-1, y)
NW: (x-1, y-1)
NE: (x,   y-1)
```

**Odd rows (y % 2 == 1):**
```
E:  (x+1, y)
SE: (x+1, y+1)
SW: (x,   y+1)
W:  (x-1, y)
NW: (x,   y-1)
NE: (x+1, y-1)
```

### Implementation

```csharp
public static class GridSystem
{
    // Offset tables for even rows
    private static readonly int[] DX_EVEN = {  1,  0, -1, -1, -1,  0 };
    private static readonly int[] DY_EVEN = {  0,  1,  1,  0, -1, -1 };
    
    // Offset tables for odd rows
    private static readonly int[] DX_ODD = {  1,  1,  0, -1,  0,  1 };
    private static readonly int[] DY_ODD = {  0,  1,  1,  0, -1, -1 };
    
    public static (int nx, int ny) GetNeighborCoords(int x, int y, HexDirection dir)
    {
        bool isOddRow = (y % 2 == 1);
        int[] dx = isOddRow ? DX_ODD : DX_EVEN;
        int[] dy = isOddRow ? DY_ODD : DY_EVEN;
        
        int d = (int)dir;
        return (x + dx[d], y + dy[d]);
    }
    
    public static int GetNeighborIndex(int x, int y, int width, HexDirection dir)
    {
        var (nx, ny) = GetNeighborCoords(x, y, dir);
        return ny * width + nx;
    }
    
    public static bool IsValidCoord(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
```

---

## Distance Calculation

### Hex Distance (Axial Space)

Hex distance is NOT simple Euclidean. Convert to axial coordinates first:

```csharp
// Convert offset to axial
public static (int q, int r) OffsetToAxial(int x, int y)
{
    int q = x - (y - (y % 2)) / 2;
    int r = y;
    return (q, r);
}

// Hex distance in axial space
public static int HexDistance(int x1, int y1, int x2, int y2)
{
    var (q1, r1) = OffsetToAxial(x1, y1);
    var (q2, r2) = OffsetToAxial(x2, y2);
    
    int dq = Math.Abs(q1 - q2);
    int dr = Math.Abs(r1 - r2);
    int ds = Math.Abs((q1 + r1) - (q2 + r2));
    
    return Math.Max(Math.Max(dq, dr), ds);
}
```

---

## Edge Encoding

### Edge Storage Format

**For directional features (rivers):**
```csharp
byte riverFlowIn;   // 6 bits (one per direction): which edges water enters from
byte riverFlowOut;  // 3 bits: direction water exits (0-5), 5 bits spare
```

**For bidirectional features (roads):**
```csharp
byte roadEdges;     // 6 bits (one per direction): connections to neighbors
```

### Validation Rule

If tile A has feature exiting in direction D:
```
Tile B (neighbor in direction D) MUST have feature entering from opposite of D.

Example:
  Tile A: river exits EAST (direction 0)
  Tile B: MUST have river entering from WEST (direction 3)
  
Opposite direction = (direction + 3) % 6
```

---

## Pathfinding

### A* on Hex Grid

```csharp
public static List<int> FindPath(int start, int goal, int width, int height, 
                                  Func<int, int, float> costFunc)
{
    var openSet = new PriorityQueue<int, float>();
    var cameFrom = new Dictionary<int, int>();
    var gScore = new Dictionary<int, float>();
    
    openSet.Enqueue(start, 0);
    gScore[start] = 0;
    
    while (openSet.Count > 0)
    {
        int current = openSet.Dequeue();
        if (current == goal)
            return ReconstructPath(cameFrom, current);
        
        int cx = current % width;
        int cy = current / width;
        
        // Check all 6 hex neighbors
        for (int d = 0; d < 6; d++)
        {
            var (nx, ny) = GetNeighborCoords(cx, cy, (HexDirection)d);
            if (!IsValidCoord(nx, ny, width, height))
                continue;
            
            int neighbor = ny * width + nx;
            float cost = costFunc(current, neighbor);
            float tentativeG = gScore[current] + cost;
            
            if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
            {
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                float f = tentativeG + HexDistance(nx, ny, goal % width, goal / width);
                openSet.Enqueue(neighbor, f);
            }
        }
    }
    
    return null; // No path found
}
```

---

## Flood Fill

### Hex Flood Fill (for lakes, regions, etc.)

```csharp
public static HashSet<int> FloodFill(int startIndex, int width, int height, 
                                      Func<int, bool> isValid)
{
    var result = new HashSet<int>();
    var queue = new Queue<int>();
    
    queue.Enqueue(startIndex);
    result.Add(startIndex);
    
    while (queue.Count > 0)
    {
        int idx = queue.Dequeue();
        int x = idx % width;
        int y = idx / width;
        
        // Check all 6 hex neighbors
        for (int d = 0; d < 6; d++)
        {
            var (nx, ny) = GetNeighborCoords(x, y, (HexDirection)d);
            if (!IsValidCoord(nx, ny, width, height))
                continue;
            
            int nidx = ny * width + nx;
            if (result.Contains(nidx))
                continue;
            
            if (isValid(nidx))
            {
                result.Add(nidx);
                queue.Enqueue(nidx);
            }
        }
    }
    
    return result;
}
```

---

## Rendering Coordinates

### World Space Conversion

For rendering hexes in 3D:

```csharp
public static Vector3 TileToWorldPosition(int x, int y, float hexSize = 1.0f)
{
    float width = hexSize * 2.0f;
    float height = hexSize * Mathf.Sqrt(3);
    
    float worldX = x * width * 0.75f;
    float worldZ = y * height;
    
    // Offset odd rows
    if (y % 2 == 1)
        worldX += width * 0.375f;
    
    return new Vector3(worldX, 0, worldZ);
}
```

---

## Common Operations

### Get All Neighbors

```csharp
public static int[] GetAllNeighbors(int x, int y, int width, int height)
{
    var neighbors = new List<int>();
    
    for (int d = 0; d < 6; d++)
    {
        var (nx, ny) = GetNeighborCoords(x, y, (HexDirection)d);
        if (IsValidCoord(nx, ny, width, height))
        {
            neighbors.Add(ny * width + nx);
        }
    }
    
    return neighbors.ToArray();
}
```

### Get Opposite Direction

```csharp
public static HexDirection GetOpposite(HexDirection dir)
{
    return (HexDirection)(((int)dir + 3) % 6);
}
```

### Ring of Tiles

Get all tiles at distance N:

```csharp
public static int[] GetRing(int centerX, int centerY, int radius, 
                             int width, int height)
{
    var ring = new List<int>();
    
    // Algorithm: Start at distance, walk around perimeter
    // (Implementation omitted for brevity - see reference code)
    
    return ring.ToArray();
}
```

---

## Testing & Validation

### Unit Tests Required

1. **Neighbor symmetry:** If B is neighbor of A in direction D, then A is neighbor of B in opposite(D)
2. **Distance:** HexDistance(A, B) == HexDistance(B, A)
3. **Bounds:** GetNeighbor never returns invalid coordinates
4. **Ring count:** Ring of radius R contains 6*R tiles (except at edges)

---

## Migration from Square Grid

### Changes Required

**Before (Square):**
```csharp
for (int dir = 0; dir < 8; dir++)
{
    int nx = x + DX[dir];
    int ny = y + DY[dir];
}
```

**After (Hex):**
```csharp
for (int dir = 0; dir < 6; dir++)
{
    var (nx, ny) = GridSystem.GetNeighborCoords(x, y, (HexDirection)dir);
}
```

### Affected Modules
- Coast (flood fill, shelf detection)
- Hydrology (flow direction, accumulation)
- Features (placement, spacing)
- Any module using neighbor calculations

---

## Performance Notes

- Neighbor calculation: O(1) - table lookup
- Distance calculation: O(1) - arithmetic
- Pathfinding: O(N log N) - same as square grid
- Flood fill: O(N) - same as square grid

Hex grid is NOT slower than square grid for most operations.

---

## References

- Red Blob Games: https://www.redblobgames.com/grids/hexagons/
- Offset Coordinates: Best for rectangular storage
- Axial Coordinates: Best for distance/math
- Cube Coordinates: Best for advanced algorithms

We use **Offset** for storage, convert to **Axial** when needed for distance.
