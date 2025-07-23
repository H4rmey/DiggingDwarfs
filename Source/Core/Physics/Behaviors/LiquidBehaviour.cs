using System;
using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for liquid pixels that combines physics and movement logic
/// Based on the original FluidPhysicsBehavior and LiquidFlowBehavior
/// </summary>
public class LiquidBehaviour : IPixelBehaviour
{
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Statistics = PhysicsStatistics.Liquid;
        pixel.Enforcers = PhysicsEnforcers.Liquid;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Apply enforcers
        pixel.Enforcers.ApplyGravity(pixel);
        pixel.Enforcers.ApplyMomentum(pixel);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Liquids fall unless they've been stopped by enforcers
        return !pixel.Statistics.CancelHorizontalMotion && pixel.Statistics.Mass > 0;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        int x = origin.X;
        int y = origin.Y;

        // First check the pixel below 
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            var belowPixel = chunk.pixels[origin.X, origin.Y + 1];
            if (belowPixel.IsEmpty(pixel))
            {
                pixel.Enforcers.ApplyMomentum(pixel);
                return (origin, new Vector2I(origin.X, origin.Y + 1));    
            }
        }

        // Apply flow behavior using enforcers
        pixel.Enforcers.ApplyFlow(pixel, origin, chunk);

        // Check for sudden stop using enforcers
        if (pixel.Enforcers.EnforceStop(pixel))
        {
            return (origin, origin);
        }

        // If you cannot go below, flow to the sides based on flow resistance
        List<Vector2I> coords = new List<Vector2I>();
        int flowRange = (int)pixel.Statistics.FlowResistance;
        for (int i = 1; i < flowRange; i++)
        {
            coords.Add(new Vector2I(i, 0));
        }

        bool doLeftFirst = GD.RandRange(0, 1) == 0;
        Vector2I direction = doLeftFirst ? Vector2I.Left : Vector2I.Right;
        
        var (Current, Next) = pixel.FindNextPixelPosition(origin, coords, chunk, direction);
        if (Current != Next) return (Current, Next);
        
        direction = !doLeftFirst ? Vector2I.Left : Vector2I.Right;
        (Current, Next) = pixel.FindNextPixelPosition(origin, coords, chunk, direction);
        
        return (Current, Next);
    }
}