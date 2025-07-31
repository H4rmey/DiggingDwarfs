using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for structure pixels that act as immovable barriers
/// These pixels have high mass and density but never move, and provide stability to surrounding solid pixels
/// </summary>
public class StructureBehaviour : IPixelBehaviour
{
    // Cache for structure calculations to avoid recalculating every frame
    private static readonly Dictionary<Vector2I, (int width, int height, float horizontalStability, float verticalStability)> _structureCache = new();
    private static int _cacheUpdateFrame = 0;
    
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Structure;
        pixel.Type = PixelType.Structure;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Structures are always static - force stop all movement
        pixel.Physics = pixel.Physics with
        {
            IsFalling = false,
            Momentum = 0,
            Velocity = Vector2I.Zero,
            CancelHorizontalMotion = true,
            MomentumDirection = Vector2I.Zero
        };
    }

    /// <summary>
    /// Calculates and applies stability improvements to surrounding solid pixels
    /// </summary>
    public void ApplyStabilityToSurroundingPixels(Vector2I origin, PixelChunk chunk, PixelElement structurePixel)
    {
        // Clear cache periodically to avoid memory buildup and handle dynamic structures
        if (_cacheUpdateFrame++ > 1000)
        {
            _structureCache.Clear();
            _cacheUpdateFrame = 0;
        }

        // Get or calculate structure dimensions and stability values
        var structureData = GetStructureStability(origin, chunk);
        
        // Apply stability to all surrounding pixels (8-directional)
        Vector2I[] surroundingOffsets = new Vector2I[]
        {
            new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, -1),
            new Vector2I(-1, 0),                       new Vector2I(1, 0),
            new Vector2I(-1, 1),  new Vector2I(0, 1),  new Vector2I(1, 1)
        };

        foreach (var offset in surroundingOffsets)
        {
            Vector2I checkPos = origin + offset;
            
            if (!chunk.IsInBounds(checkPos.X, checkPos.Y))
                continue;

            var adjacentPixel = chunk.pixels[checkPos.X, checkPos.Y];
            
            // Only apply stability to solid pixels
            if (adjacentPixel?.Type == PixelType.Solid)
            {
                // Apply stability bonuses based on structure dimensions
                adjacentPixel.Physics = adjacentPixel.Physics with
                {
                    HorizontalStability = Mathf.Min(1.0f, adjacentPixel.Physics.HorizontalStability + structureData.horizontalStability),
                    VerticalStability = Mathf.Min(1.0f, adjacentPixel.Physics.VerticalStability + structureData.verticalStability),
                    Stability = structureData.horizontalStability + structureData.verticalStability
                };
            }
        }
    }

    /// <summary>
    /// Gets structure stability values, using cache when possible
    /// </summary>
    private (int width, int height, float horizontalStability, float verticalStability) GetStructureStability(Vector2I origin, PixelChunk chunk)
    {
        if (_structureCache.TryGetValue(origin, out var cachedData))
        {
            return cachedData;
        }

        var dimensions = CalculateStructureDimensions(origin, chunk);
        var stability = CalculateStabilityFromDimensions(dimensions.width, dimensions.height);
        
        var result = (dimensions.width, dimensions.height, stability.horizontalStability, stability.verticalStability);
        _structureCache[origin] = result;
        
        return result;
    }

    /// <summary>
    /// Uses flood fill to calculate the dimensions of the structure this pixel belongs to
    /// </summary>
    private (int width, int height) CalculateStructureDimensions(Vector2I origin, PixelChunk chunk)
    {
        var visited = new HashSet<Vector2I>();
        var structurePixels = new List<Vector2I>();
        var queue = new Queue<Vector2I>();
        
        queue.Enqueue(origin);
        visited.Add(origin);

        // Flood fill to find all connected structure pixels
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            structurePixels.Add(current);

            // Check 4-directional connectivity (not diagonal for structure integrity)
            Vector2I[] directions = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
            
            foreach (var direction in directions)
            {
                var neighbor = current + direction;
                
                if (!chunk.IsInBounds(neighbor.X, neighbor.Y) || visited.Contains(neighbor))
                    continue;

                var neighborPixel = chunk.pixels[neighbor.X, neighbor.Y];
                if (neighborPixel?.Type == PixelType.Structure)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Calculate bounding box dimensions
        if (structurePixels.Count == 0)
            return (1, 1);

        int minX = structurePixels.Min(p => p.X);
        int maxX = structurePixels.Max(p => p.X);
        int minY = structurePixels.Min(p => p.Y);
        int maxY = structurePixels.Max(p => p.Y);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        return (width, height);
    }

    /// <summary>
    /// Calculates stability bonuses based on structure dimensions
    /// Broader structures give better horizontal stability
    /// Shorter structures give better vertical stability
    /// </summary>
    private (float horizontalStability, float verticalStability) CalculateStabilityFromDimensions(int width, int height)
    {
        // Base stability values
        const float baseStability = 0.1f;
        const float maxStability = 0.5f;
        
        // Horizontal stability increases with width (broader = more stable horizontally)
        // Use logarithmic scaling to prevent excessive bonuses
        float horizontalStability = baseStability + (Mathf.Log(width + 1) * 0.15f);
        horizontalStability = Mathf.Min(horizontalStability, maxStability);
        
        // Vertical stability increases with inverse of height (shorter = more stable vertically)
        // Taller structures are less stable vertically
        float heightFactor = 1.0f / Mathf.Max(height, 1);
        float verticalStability = baseStability + (heightFactor * 0.3f);
        verticalStability = Mathf.Min(verticalStability, maxStability);
        
        return (horizontalStability, verticalStability);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Structure pixels never fall - they are immovable
        return false;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // Apply stability to surrounding pixels before returning (structures don't move)
        ApplyStabilityToSurroundingPixels(origin, chunk, pixel);
        
        // Structure pixels never move - they act as barriers
        return (origin, origin);
    }
}