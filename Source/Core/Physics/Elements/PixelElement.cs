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

    // Unified physics system
    public PhysicsHelper Physics { get; set; }

    // Unified behavior component
    public IPixelBehaviour Behaviour { get; set; }
    public IVisualBehavior VisualBehavior { get; set; }

    public PixelElement()
    {
        BaseColor = Colors.Purple;
        Color     = Colors.Purple;
        Type      = PixelType.Empty;
        
        // Initialize the physics system with default empty values
        Physics = PhysicsHelper.Empty;
        
        // Apply random resistance coefficient for backward compatibility
        float randomFriction = (float)GD.RandRange(0.0f, 1.0f);
        Physics = Physics with
        {
            HorizontalStability = randomFriction,
            VerticalStability = randomFriction
        };
    }
    
    public virtual bool IsEmpty(PixelElement element)
    {
        return element.Physics.Mass > Physics.Mass;
    }
    
    public virtual (Vector2I Current, Vector2I Next) GetSwapPosition(PixelWorld world, PixelChunk chunk, Vector2I origin)
    {
        return Behaviour?.GetSwapPosition(world, chunk, this, origin) ?? (origin, origin);
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

    public virtual void ExecuteTopBottomLeftRight(PixelWorld world, PixelChunk chunk, Vector2I origin, PixelAction action)
    {
        // Define all 8 surrounding positions (including diagonals)
        Vector2I[] surroundingPositions = new Vector2I[]
        {
            new Vector2I(0, -1),  // top
            new Vector2I(-1, 0),  // left
            new Vector2I(1, 0),   // right
            new Vector2I(0, 1),   // bottom
        };

        foreach (Vector2I offset in surroundingPositions)
        {
            Vector2I checkPos = origin + offset;
            
            // Skip if position is out of bounds
            if (!chunk.IsInBound(checkPos))
                continue;

            // Get the pixel at this position - for proof-of-concept, skip type checking
            PixelElement pixel = world.GetPixelElementAt(checkPos);
            if (pixel != null)
            {
                // Invoke the action on the found pixel
                action?.Invoke(pixel, checkPos);
            }
        }
    }

    public virtual void ExecuteSurroundingPixel(PixelWorld world, PixelChunk chunk, Vector2I origin, PixelAction action)
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
            if (!chunk.IsInBound(checkPos))
                continue;

            // Get the pixel at this position - for proof-of-concept, skip type checking
            PixelElement pixel = world.GetPixelElementAt(checkPos);
            if (pixel != null)
            {
                // Invoke the action on the found pixel
                action?.Invoke(pixel, checkPos);
            }
        }
    }
    
    // This function expects that the origin passed has already been converted to the worldposition beforehand by calling chunk.ToWorldPosition
    // TODO: Figure out if this is the way to go or let the function convert it to worldposition
    public (Vector2I Current, Vector2I Next) FindNextPixelPosition(PixelWorld world, PixelChunk chunk, Vector2I origin, List<Vector2I> coords, Vector2I direction, int randomRangeOffset = 10)
    {
        // Store the first valid empty position we find
        Vector2I? firstValidPosition = null;
        
        //origin = chunk.ToWorldPosition(origin);
        Vector2I nextPos = new Vector2I(origin.X, origin.Y + 1);

        foreach (Vector2I coord in coords)
        {
            Vector2I targetPos = origin + coord * direction;

            // Skip if position is out of bounds
            if (!world.IsInBound(targetPos))
                continue;

            PixelElement pixel = world.GetPixelElementAt(targetPos);

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