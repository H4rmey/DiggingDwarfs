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
    private int maxCheckTimes = 5000;
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Liquid;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Apply physics
        pixel.Physics.ApplyGravity(pixel);
        pixel.Physics.ApplyMomentum(pixel);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Liquids fall unless they've been stopped by physics
        return !pixel.Physics.CancelHorizontalMotion && pixel.Physics.Mass > 0;
    }

    /// <summary>
    /// Determines the next position for a liquid pixel based on physics rules and the pixel's statistics.
    /// Returns a tuple containing the current position and the next position.
    /// </summary>
    /// <param name="origin">The current position of the pixel</param>
    /// <param name="chunk">The pixel chunk containing the pixel</param>
    /// <param name="pixel">The pixel element being processed</param>
    /// <returns>A tuple with (Current position, Next position)</returns>
    /// TODO: Water currently keeps chunks active, i should change it so it when a pixel has attempted about 10 positions it will stop looking or something like that
    /// active chunks cost compute power
    public (Vector2I Current, Vector2I Next) GetSwapPosition(PixelWorld world, PixelChunk chunk, PixelElement pixel, Vector2I origin)
    {
        // If a pixel is falling, ensure vertical motion is allowed
        // This is the complement to the IsFalling/CancelHorizontalMotion relationship:
        // - IsFalling=true requires CancelHorizontalMotion=true (enforced in PhysicsHelper)
        // - IsFalling=true requires CancelVerticalMotion=false (enforced here)
        if (pixel.Physics.IsFalling) pixel.Physics = pixel.Physics with { CancelVerticalMotion = false };

        // If vertical motion is canceled, the pixel stays in place
        if (pixel.Physics.CancelVerticalMotion) return (origin, origin);

        origin = chunk.ToWorldPosition(origin);
        Vector2I nextPos = new Vector2I(origin.X, origin.Y + 1);
        
        // 1. Check if you can place a pixel directly below
        if (world.IsInBound(nextPos))
        {
            PixelElement belowPixel = world.GetPixelElementAt(nextPos);
            if (belowPixel.IsEmpty(pixel))
            {
                // Calculate momentum as the pixel falls downward
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, nextPos);
            }
        }

        if (pixel.Physics.CancelVerticalMotion) return (origin, origin);
        
        // If can't move directly down, calculate how the liquid should flow laterally
        // Apply flow physics to the liquid (handles spread patterns and momentum)
        pixel.Physics.ApplyFlow(world, chunk, pixel, origin);

        // Check if the pixel should stop moving (based on physics thresholds like friction)
        // This simulates how real liquids can stop flowing in certain conditions
        if (pixel.Physics.DoCancelHorizontalMotion(pixel, pixel.Physics.HorizontalStability))
        {
            return (origin, origin);
        }

        // Generate a list of possible horizontal offsets based on flow resistance
        List<Vector2I> coords = new List<Vector2I>();
        for (int i = 1; i < pixel.Physics.Viscosity; i++)
        {
            coords.Add(new Vector2I(i, 0)); // Horizontal offsets (will be multiplied by direction)
        }

        // Randomly decide whether to check left or right first for more natural liquid behavior
        // This prevents all liquid from always flowing in the same direction
        bool doLeftFirst = GD.RandRange(0, 1) == 0;
        Vector2I direction = doLeftFirst ? Vector2I.Left : Vector2I.Right;
        
        // Try to find a valid position in the first chosen direction
        var (Current, Next) = pixel.FindNextPixelPosition(world, chunk, origin, coords, direction, 6);
        if (Current != Next) return (Current, Next); // Return immediately if a valid move is found
        
        // If first direction fails, try the opposite direction
        direction = !doLeftFirst ? Vector2I.Left : Vector2I.Right;
        (Current, Next) = pixel.FindNextPixelPosition(world, chunk, origin, coords, direction, 6);
        
        // Return final result - if no movement is possible, Current and Next will be the same
        return (Current, Next);
    }
}