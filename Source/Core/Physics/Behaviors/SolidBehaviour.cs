using System;
using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for solid/granular pixels that combines physics and movement logic
/// Based on the original GranularPhysicsBehavior and FallingMovementBehavior
/// </summary>
public class SolidBehaviour : IPixelBehaviour
{
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Solid;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Apply physics
        pixel.Physics.ApplyGravity(pixel);
        pixel.Physics.ApplyMomentum(pixel);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Solids fall unless they've been stopped by physics
        return !pixel.Physics.CancelHorizontalMotion && pixel.Physics.Mass > 0;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel, PixelWorld pixelWorld)
    {
        if (pixel.Physics.CancelVerticalMotion) return (origin, origin);

        // 1. Check if you can place a pixel directly below
        Vector2I belowPos = new Vector2I(origin.X, origin.Y + 1);
        
        // Check if position is within current chunk bounds
        if (chunk.IsInBounds(belowPos.X, belowPos.Y))
        {
            PixelElement belowPixel = chunk.pixels[belowPos.X, belowPos.Y];
            if (belowPixel.IsEmpty(pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };

                // The pixel below the current pixel should be falling, as it carries momentum so that momentum is transfered to the pixel below it.
                chunk.pixels[belowPos.X, belowPos.Y].Physics = chunk.pixels[belowPos.X, belowPos.Y].Physics with { IsFalling = true, CancelVerticalMotion = false };

                // when a pixel falls down next to a another pixel it has a chance to "drag" the other pixel with it
                pixel.ExecuteSurroundingPixel(origin, chunk, (adjacentPixel, pos) =>
                {
                    if (GD.RandRange(0.0f, 1.0f) < pixel.Physics.HorizontalStability)
                    {
                        adjacentPixel.Physics = adjacentPixel.Physics with { CancelHorizontalMotion = false, CancelVerticalMotion = false };
                    }
                });

                // finally apply the new momentum
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, belowPos);
            }
        }
        else
        {
            // Position is out of current chunk bounds, check using PixelWorld
            Vector2I worldOrigin = pixelWorld.ChunkToWorldCoordinate(origin, chunk);
            Vector2I worldBelowPos = worldOrigin + new Vector2I(0, 1);
            
            if (pixelWorld.CanMoveToWorldPosition(worldBelowPos, pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, belowPos);
            }
        }

        // 1.1 Handle sudden stop using enforcers
        if (pixel.Physics.CancelHorizontalMotion) return (origin, origin);
        if (pixel.Physics.DoCancelHorizontalMotion(pixel, pixel.Physics.HorizontalStability)) return (origin, origin);
        
        // 2. If there is no place directly below -> check the belowLeft and belowRight side in a random order
        bool tryLeftFirst = GD.RandRange(0, 1) == 0;
        Vector2I firstDirection = tryLeftFirst ? new Vector2I(-1, 1) : new Vector2I(1, 1);
        Vector2I secondDirection = tryLeftFirst ? new Vector2I(1, 1) : new Vector2I(-1, 1);

        // Try first diagonal direction
        Vector2I firstDiagonalPos = origin + firstDirection;
        
        if (chunk.IsInBounds(firstDiagonalPos.X, firstDiagonalPos.Y))
        {
            var diagonalPixel = chunk.pixels[firstDiagonalPos.X, firstDiagonalPos.Y];
            if (diagonalPixel.IsEmpty(pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, firstDiagonalPos);
            }
        }
        else
        {
            // Position is out of current chunk bounds, check using PixelWorld
            Vector2I worldOrigin = pixelWorld.ChunkToWorldCoordinate(origin, chunk);
            Vector2I worldDiagonalPos = worldOrigin + firstDirection;
            
            if (pixelWorld.CanMoveToWorldPosition(worldDiagonalPos, pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, firstDiagonalPos);
            }
        }

        // Try second diagonal direction
        Vector2I secondDiagonalPos = origin + secondDirection;
        if (chunk.IsInBounds(secondDiagonalPos.X, secondDiagonalPos.Y))
        {
            var diagonalPixel = chunk.pixels[secondDiagonalPos.X, secondDiagonalPos.Y];
            if (diagonalPixel.IsEmpty(pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, secondDiagonalPos);
            }
        }

        // 3. If belowLeft, belowRight and below are all blocked, resolve the momentum
        pixel.Physics = pixel.Physics with { IsFalling = false };
        if (!pixel.Physics.IsFalling && pixel.Physics.Momentum > 0)
        {
            // If we haven't set a momentum direction yet (just landed), set it based on last diagonal movement
            if (pixel.Physics.MomentumDirection == Vector2I.Zero)
            {
                // Use the X component of the last diagonal movement to determine direction
                Vector2I newDirection = firstDirection.X > 0 ? Vector2I.Right : Vector2I.Left;
                pixel.Physics = pixel.Physics with { MomentumDirection = newDirection };
            }

            // Move in the stored momentum direction
            Vector2I targetPos = origin + pixel.Physics.MomentumDirection;

            if (chunk.IsInBounds(targetPos.X, targetPos.Y))
            {
                var targetPixel = chunk.pixels[targetPos.X, targetPos.Y];
                if (targetPixel.IsEmpty(pixel))
                {
                    // Decrease momentum by the HorizontalFriction
                    float newMomentum = pixel.Physics.Momentum - pixel.Physics.HorizontalStability;
                    if (newMomentum <= 0)
                    {
                        pixel.Physics = pixel.Physics with
                        {
                            Momentum = 0,
                            MomentumDirection = Vector2I.Zero
                        };
                    }
                    else
                    {
                        pixel.Physics = pixel.Physics with { Momentum = newMomentum };
                    }
                    return (origin, targetPos);
                }
            }
        }
        
        return (origin, origin);
    }
}