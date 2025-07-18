using System;
using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Movement;

/// <summary>
/// Movement behavior for liquid particles that flow and spread
/// Based on the original PixelLiquid movement logic
/// </summary>
public class LiquidFlowBehavior : IMovementBehavior
{
    private readonly float viscosity;

    public LiquidFlowBehavior(float viscosity = 8.0f)
    {
        this.viscosity = viscosity;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        int x = origin.X;
        int y = origin.Y;

        // First only check the pixel below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            var belowPixel = chunk.pixels[origin.X, origin.Y + 1];
            if (IsEmpty(belowPixel, pixel))
            {
                return (origin, new Vector2I(origin.X, origin.Y + 1));    
            }
        }

        // If you cannot go below, go to the side instead 
        List<Vector2I> coords = new List<Vector2I>();
        for (int i = 1; i < viscosity; i++)
        {
            coords.Add(new Vector2I(i, 0));
        }

        bool doLeftFirst = GD.RandRange(0, 1) == 0;
        Vector2I direction = doLeftFirst ? Vector2I.Left : Vector2I.Right;
        
        var (Current, Next) = FindNextPixelPosition(origin, coords, chunk, direction, pixel);
        if (Current != Next) return (Current, Next);
        
        direction = !doLeftFirst ? Vector2I.Left : Vector2I.Right;
        (Current, Next) = FindNextPixelPosition(origin, coords, chunk, direction, pixel);
        
        return (Current, Next);
    }

    // Helper method to find next pixel position (similar to PixelElementComposed.FindNextPixelPosition)
    private (Vector2I Current, Vector2I Next) FindNextPixelPosition(Vector2I origin, List<Vector2I> coords, PixelChunk chunk, Vector2I direction, PixelElement pixel)
    {
        Vector2I? firstValidPosition = null;

        foreach (Vector2I coord in coords)
        {
            Vector2I targetPos = origin + coord * direction;
            
            if (!chunk.IsInBounds(targetPos.X, targetPos.Y)) 
                continue;

            var targetPixel = chunk.pixels[targetPos.X, targetPos.Y];
            
            if (!IsEmpty(targetPixel, pixel)) 
                continue;
                
            firstValidPosition = targetPos;
            break;
        }

        return (origin, firstValidPosition ?? origin);
    }

    // Helper method to check if a position is empty
    private bool IsEmpty(object pixelAtPosition, PixelElement currentPixel)
    {
        if (pixelAtPosition is PixelElement composedPixel)
        {
            return composedPixel.IsEmpty(currentPixel);
        }
        return false;
    }
}