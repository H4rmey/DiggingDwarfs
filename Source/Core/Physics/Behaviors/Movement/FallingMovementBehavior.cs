using System;
using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Movement;

/// <summary>
/// Movement behavior for solid particles that fall and accumulate momentum
/// Based on the original PixelSolid movement logic
/// </summary>
public class FallingMovementBehavior : IMovementBehavior
{
    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // 1. Check if you can place a pixel directly below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            var belowPixel = chunk.pixels[origin.X, origin.Y + 1];
            if (IsEmpty(belowPixel, pixel))
            {
                if (!GetIsFalling(belowPixel)) pixel.IsFalling = true;
                pixel.Momentum += pixel.Mass; // Accumulate momentum based on mass
                
                // Use CheckSurroundingPixels to handle adjacent pixels
                pixel.CheckSurroundingPixels(origin, chunk, (adjacentPixel, pos) => {
                    if (GD.RandRange(0.0f, 1.0f) < pixel.Friction) adjacentPixel.SuddenStop = false;
                });

                return (origin, new Vector2I(origin.X, origin.Y + 1));
            }
        }

        // 1.1 Handle sudden stop
        if (pixel.SuddenStop) return (origin, origin);
        if (pixel.SetSuddenStop()) return (origin, origin);
        
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
            if (IsEmpty(diagonalPixel, pixel))
            {
                if (!GetIsFalling(diagonalPixel)) pixel.IsFalling = true;
                pixel.Momentum += pixel.Mass; // Accumulate momentum based on mass
                return (origin, origin + firstDirection);
            }
        }

        // Try second diagonal direction
        if (chunk.IsInBounds(origin.X + secondDirection.X, origin.Y + secondDirection.Y))
        {
            var diagonalPixel = chunk.pixels[origin.X + secondDirection.X, origin.Y + secondDirection.Y];
            if (IsEmpty(diagonalPixel, pixel))
            {
                if (!GetIsFalling(diagonalPixel)) pixel.IsFalling = true;
                pixel.Momentum += pixel.Mass; // Accumulate momentum based on mass
                return (origin, origin + secondDirection);
            }
        }
        
        // 3. If belowLeft, belowRight and below are all empty Then resolve the momentum
        if (!pixel.IsFalling && pixel.Momentum > 0)
        {
            // If we haven't set a momentum direction yet (just landed), set it based on last diagonal movement
            if (pixel.MomentumDirection == Vector2I.Zero)
            {
                // Use the X component of the last diagonal movement to determine direction
                pixel.MomentumDirection = firstDirection.X > 0 ? Vector2I.Right : Vector2I.Left;
            }

            // Move in the stored momentum direction
            Vector2I targetPos = origin + pixel.MomentumDirection;
            
            if (chunk.IsInBounds(targetPos.X, targetPos.Y))
            {
                var targetPixel = chunk.pixels[targetPos.X, targetPos.Y];
                if (IsEmpty(targetPixel, pixel))
                {
                    pixel.Momentum--;
                    if (pixel.Momentum <= 0)
                    {
                        pixel.Momentum = 0;
                        pixel.MomentumDirection = Vector2I.Zero; // Reset direction when momentum is used up
                    }
                    return (origin, targetPos);
                }
            }
        }
        else if (pixel.IsFalling)
        {
            // We were falling but couldn't move, so we've landed
            pixel.IsFalling = false;
            // Don't reset momentum here, it's already accumulated during falling
        }
        
        return (origin, origin);
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

    // Helper method to get IsFalling property
    private bool GetIsFalling(object pixelAtPosition)
    {
        if (pixelAtPosition is PixelElement composedPixel)
        {
            return composedPixel.IsFalling;
        }
        return false;
    }

    // Temporary method to convert to old PixelElement for compatibility during transition
    // Temporary conversion method removed - no longer needed since old classes are deleted
}