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

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // if a pixel is falling, make sure the vertical motion is allowed.
        if (pixel.Physics.IsFalling) pixel.Physics = pixel.Physics with { CancelVerticalMotion = false };

        if (pixel.Physics.CancelVerticalMotion) return (origin, origin);

        // 1. Check if you can place a pixel directly below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            var belowPixel = chunk.pixels[origin.X, origin.Y + 1];
            if (belowPixel.IsEmpty(pixel))
            {

                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);

                // The pixel below the current pixel should be falling, as it carries momentum so that momentum is transfered to the pixel below it.
                // TODO: make it so the IsFalling is only set based on a calculation with mass, momentum and friction.
                chunk.pixels[origin.X, origin.Y + 1].Physics = chunk.pixels[origin.X, origin.Y + 1].Physics with { IsFalling = true, CancelVerticalMotion = false };


                // Use CheckSurroundingPixels to handle adjacent pixels
                // Copy the horizontalFriction value to use in the lambda to avoid 'this' capture issues
                float horizontalFriction = pixel.Physics.HorizontalFriction;
                
                pixel.CheckSurroundingPixels(origin, chunk, (adjacentPixel, pos) =>
                {
                    if (GD.RandRange(0.0f, 1.0f) < horizontalFriction)
                    {
                        adjacentPixel.Physics = adjacentPixel.Physics with { CancelHorizontalMotion = false, CancelVerticalMotion = false };
                    }
                });

                return (origin, new Vector2I(origin.X, origin.Y + 1));
            }
        }

        // 1.1 Handle sudden stop using enforcers
        // suddenstop is used to restrict sideways momentum
        // TODO: rename suddenstop to CancelHorizontalMomentum and add a CancelVerticalMomentum boolean (which is currently unused)
        if (pixel.Physics.CancelHorizontalMotion) return (origin, origin);
        if (pixel.Physics.EnforceStop(pixel)) return (origin, origin);
        
        // 2. If there is no place directly below -> check the belowLeft and belowRight side in a random order
        var diagonalPositions = new List<Vector2I>
        {
            new Vector2I(-1, 1), // belowLeft
            new Vector2I(1, 1)   // belowRight
        };

        // Randomly choose which diagonal to try first
        bool tryLeftFirst = GD.RandRange(0, 1) == 0;
        Vector2I firstDirection = tryLeftFirst ? new Vector2I(-1, 1) : new Vector2I(1, 1);
        Vector2I secondDirection = tryLeftFirst ? new Vector2I(1, 1) : new Vector2I(-1, 1);

        // Try first diagonal direction
        if (chunk.IsInBounds(origin.X + firstDirection.X, origin.Y + firstDirection.Y))
        {
            var diagonalPixel = chunk.pixels[origin.X + firstDirection.X, origin.Y + firstDirection.Y];
            if (diagonalPixel.IsEmpty(pixel))
            {
                // The pixel below the current pixel should be falling, as it carries momentum so that momentum is transfered to the pixel below it.
                // TODO: make it so the IsFalling is only set based on a calculation with mass, momentum and friction.
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, origin + firstDirection);
            }
        }

        // Try second diagonal direction
        if (chunk.IsInBounds(origin.X + secondDirection.X, origin.Y + secondDirection.Y))
        {
            var diagonalPixel = chunk.pixels[origin.X + secondDirection.X, origin.Y + secondDirection.Y];
            if (diagonalPixel.IsEmpty(pixel))
            {
                // The pixel below the current pixel should be falling, as it carries momentum so that momentum is transfered to the pixel below it.
                // TODO: make it so the IsFalling is only set based on a calculation with mass, momentum and friction.
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
                pixel.Physics.ApplyMomentum(pixel);
                
                return (origin, origin + secondDirection);
            }
        }
        
        // 3. If belowLeft, belowRight and below are all blocked, resolve the momentum
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
                    float newMomentum = pixel.Physics.Momentum - 1;
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
        else if (pixel.Physics.IsFalling)
        {
            // We were falling but couldn't move, so we've landed
            pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };
            // Don't reset momentum here, it's already accumulated during falling
        }
        
        return (origin, origin);
    }
}