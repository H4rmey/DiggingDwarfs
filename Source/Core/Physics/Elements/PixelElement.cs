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
    
    // Core properties
    public bool IsFalling;
    public PixelState State; 
    public Color BaseColor;
    public Color Color;
    public float Mass;
    public Vector2I Velocity;
    public float Momentum;
    public float Friction;
    public bool SuddenStop = false;
    public Vector2I MomentumDirection = Vector2I.Zero;

    // Behavior components
    public IMovementBehavior MovementBehavior { get; set; }
    public IPhysicsBehavior PhysicsBehavior { get; set; }
    public IVisualBehavior VisualBehavior { get; set; }

    public PixelElement()
    {
        BaseColor = Colors.Purple;
        Color     = Colors.Purple;
        State     = PixelState.Empty; 
        IsFalling = false;
        Mass      = 0;
        Velocity  = Vector2I.Zero;
        Momentum  = 0;
        Friction  = (float)GD.RandRange(0.0f, 1.0f);
    }
    
    public virtual bool IsEmpty(PixelElement element)
    {
        return element.Mass > Mass;
    }
    
    public virtual (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        return MovementBehavior?.GetSwapPosition(origin, chunk, this) ?? (origin, origin);
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

    public virtual bool SetSuddenStop()
    {
        if (GD.RandRange(0.0f, 1.0f) < Friction)
        {
            SuddenStop = true;
            Momentum = 0.0f;
            MomentumDirection = Vector2I.Zero;
            return true;
        }
        return false;
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
                // For now, only work with composed pixels in the proof-of-concept
                // We'll handle mixed types later in full implementation
                continue;
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
            
            // For proof-of-concept, use the existing PixelElement IsEmpty method
            // We'll handle the composition transition properly in the full implementation
            if (pixel is PixelElement composedPixel)
            {
                bool isEmpty = composedPixel.IsEmpty(this);
                if (!isEmpty)
                    continue;
            }
            else
            {
                // Skip unknown pixel types
                continue;
            }
                
            // Found a valid empty position
            firstValidPosition = targetPos;
            break; // Exit loop after finding first valid position
        }

        // Return the first valid position found, or origin if none found
        return (origin, firstValidPosition ?? origin);
    }

    // Old conversion method removed - no longer needed
}