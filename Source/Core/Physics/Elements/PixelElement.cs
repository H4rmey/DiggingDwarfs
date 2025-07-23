using System.Collections.Generic;
using System.Linq;
using Godot;
using System;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Physics.Elements;

/// <summary>
/// A composition-based pixel element that uses behavior components instead of inheritance
/// </summary>
public class PixelElement
{
    public delegate void PixelAction(PixelElement pixel, Vector2I position);
    
    // Core visual properties
    public PixelType Type;
    public Color BaseColor;
    public Color Color;

    // Composition-based physics system
    public PhysicsStatistics Statistics { get; set; }
    public PhysicsEnforcers Enforcers { get; set; }

    // Unified behavior component
    public IPixelBehaviour Behaviour { get; set; }
    public IVisualBehavior VisualBehavior { get; set; }

    public PixelElement()
    {
        BaseColor = Colors.Purple;
        Color     = Colors.Purple;
        Type      = PixelType.Empty;
        
        // Initialize the new physics system with default empty values
        Statistics = PhysicsStatistics.Empty;
        Enforcers = PhysicsEnforcers.Empty;
        
        // Apply random resistance coefficient for backward compatibility
        float randomFriction = (float)GD.RandRange(0.0f, 1.0f);
        Statistics = Statistics with
        {
            HorizontalFriction = randomFriction,
            VerticalFriction = randomFriction
        };
    }
    
    public virtual bool IsEmpty(PixelElement element)
    {
        return element.Statistics.Mass > Statistics.Mass;
    }
    
    public virtual (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        return Behaviour?.GetSwapPosition(origin, chunk, this) ?? (origin, origin);
    }

    public virtual void SetRandomColor()
    {
        VisualBehavior?.SetRandomColor(this);
    }

    public virtual PixelElement Clone()
    {
        PixelElement clone = (PixelElement)MemberwiseClone();
        return clone;
    }

    public virtual void CheckSurroundingPixels(Vector2I origin, PixelChunk chunk, PixelAction action)
    {
        // Define all 8 surrounding positions (including diagonals)
        Vector2I[] surroundingPositions = new Vector2I[]
        {
            new Vector2I(-1, -1), // top-left
            new Vector2I(0, -1),  // top
            new Vector2I(1, -1),  // top-right
            new Vector2I(-1, 0),  // left
            new Vector2I(1, 0),   // right
            new Vector2I(-1, 1),  // bottom-left
            new Vector2I(0, 1),   // bottom
            new Vector2I(1, 1)    // bottom-right
        };

        foreach (Vector2I offset in surroundingPositions)
        {
            Vector2I checkPos = origin + offset;
            
            // Skip if position is out of bounds
            if (!chunk.IsInBounds(checkPos.X, checkPos.Y))
                continue;

            // Get the pixel at this position - for proof-of-concept, skip type checking
            var pixel = chunk.pixels[checkPos.X, checkPos.Y];
            if (pixel != null)
            {
                // Invoke the action on the found pixel
                action?.Invoke(pixel, checkPos);
            }
        }
    }
    
    public (Vector2I Current, Vector2I Next) FindNextPixelPosition(Vector2I origin, List<Vector2I> coords, PixelChunk chunk, Vector2I direction, int randomRangeOffset = 10)
    {
        // Store the first valid empty position we find
        Vector2I? firstValidPosition = null;

        foreach (Vector2I coord in coords)
        {
            Vector2I targetPos = origin + coord * direction;

            // Skip if position is out of bounds
            if (!chunk.IsInBounds(targetPos.X, targetPos.Y))
                continue;

            var pixel = chunk.pixels[targetPos.X, targetPos.Y];

            // exit on the first empty pixel that we find

            if (pixel.IsEmpty(this))
            {
                firstValidPosition = targetPos;
                break;
            }
            else if (pixel.Type != PixelType.Liquid )
            {
                break;
            }
            
        }

        // Return the first valid position found, or origin if none found
        return (origin, firstValidPosition ?? origin);
    }

    // Old conversion method removed - no longer needed
}