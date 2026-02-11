# GridHelpers Utility

## Purpose
Provides grid-based algorithms for 2D array manipulation commonly needed in map generation.

## Core Functions

### Coordinate Conversion
- `ToIndex(x, y, width)` - Convert 2D coords to 1D array index
- `ToCoords(index, width)` - Convert 1D array index to 2D coords
- `IsInBounds(x, y, width, height)` - Check if coordinates are valid

### Neighbor Iteration
- `GetNeighbors4(x, y, width, height)` - Returns 4-connected neighbors (N,S,E,W)
- `GetNeighbors8(x, y, width, height)` - Returns 8-connected neighbors (includes diagonals)

### Flood Fill
- `FloodFill(mask, width, height, startX, startY, fillValue)` - Fill connected region
- `FloodFillEdges(mask, width, height, edgeValue, fillValue)` - Fill from all edges

### Distance Fields
- `ComputeDistanceField(sources, width, height, maxDistance)` - Manhattan distance
- `ComputeEuclideanDistanceField(sources, width, height, maxDistance)` - Euclidean distance

### Counting
- `CountNeighbors4Matching<T>(array, x, y, width, height, value)` - Count 4-neighbors with value
- `CountNeighbors8Matching<T>(array, x, y, width, height, value)` - Count 8-neighbors with value

## Usage Examples

### Finding True Ocean (Coast Module)
```csharp
// Separate edge-connected ocean from inland lakes
bool[] isWater = new bool[count];
for (int i = 0; i < count; i++)
    isWater[i] = elevationBands[i] == ElevationBandFinal.Ocean;

// Flood from edges - marks true ocean
GridHelpers.FloodFillEdges(isWater, width, height, true, true);
```

### Computing Coastal Shelf Distance
```csharp
// Distance from coastline
bool[] isLand = new bool[count];
for (int i = 0; i < count; i++)
    isLand[i] = elevationBands[i] != ElevationBandFinal.Ocean;

float[] distanceToCoast = GridHelpers.ComputeDistanceField(isLand, width, height, 100f);
```

### Moisture from Water Sources
```csharp
bool[] waterSources = new bool[count];
for (int i = 0; i < count; i++)
    waterSources[i] = isOcean[i] || isRiver[i] || isLake[i];

float[] moistureField = GridHelpers.ComputeDistanceField(waterSources, width, height, 200f);

// Invert: closer = higher moisture
for (int i = 0; i < count; i++)
    moisture[i] = 1f - (moistureField[i] / 200f);
```

### Mountain Ridge Detection
```csharp
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        int idx = GridHelpers.ToIndex(x, y, width);
        
        if (elevationRaw[idx] > mountainThreshold)
        {
            int higherNeighbors = 0;
            foreach (var (nx, ny) in GridHelpers.GetNeighbors8(x, y, width, height))
            {
                int nIdx = GridHelpers.ToIndex(nx, ny, width);
                if (elevationRaw[nIdx] > elevationRaw[idx])
                    higherNeighbors++;
            }
            
            // Local peak = potential ridge point
            if (higherNeighbors == 0)
                isMountainPeak[idx] = true;
        }
    }
}
```

## Performance Notes

- **Manhattan Distance**: O(n) where n = grid size. Fast, uses BFS.
- **Euclidean Distance**: O(n²). Slower, brute-force. Use for small grids or when accuracy matters.
- **Flood Fill**: O(n) with BFS. Very fast for region marking.
- **Neighbor Iteration**: Use `GetNeighbors4` when possible (faster than 8-way).

## Dependencies
- None (pure C# with System.Collections.Generic)

## Thread Safety
- All methods are stateless and thread-safe for read-only inputs
- Not safe for concurrent writes to the same array
