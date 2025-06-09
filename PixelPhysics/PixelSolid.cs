using System.Collections.Generic;
using Godot;
using System;

namespace SharpDiggingDwarfs;

public class PixelSolid : PixelElement
{
    // Delegate type for the function that will be executed on each surrounding pixel
    public delegate void PixelAction(PixelElement pixel, Vector2I position);

    public PixelSolid()
    {
        BaseColor = Colors.Yellow;
        Color = Colors.Orange;
        State = PixelState.Solid;
        IsFalling = true;
        Mass = 0.33f;
        Velocity = Vector2I.Zero;
        Momentum = 0; // Start with no momentum
        Friction = 0.01f; // Start with no momentum
    }

    public override (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        
        // 1. Check if you can place a pixel directly below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            PixelElement pixel = chunk.pixels[origin.X, origin.Y + 1];
            if (pixel.IsEmpty(this))
            {
                if (!pixel.IsFalling) IsFalling = true;
                Momentum += Mass; // Accumulate momentum based on mass
                
                // Use CheckSurroundingPixels to handle adjacent pixels
                CheckSurroundingPixels(origin, chunk, (adjacentPixel, pos) => {
                    if (GD.RandRange(0.0f, 1.0f) < Friction) adjacentPixel.SuddenStop = false;
                });

                return (origin, new Vector2I(origin.X, origin.Y + 1));
            }
        }

        
        // 1.1 
        if (SuddenStop) return (origin, origin);
        if (SetSuddenStop()) return (origin, origin);
        
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
            PixelElement pixel = chunk.pixels[origin.X + firstDirection.X, origin.Y + firstDirection.Y];
            if (pixel.IsEmpty(this))
            {
                if (!pixel.IsFalling) IsFalling = true;
                Momentum += Mass; // Accumulate momentum based on mass
                return (origin, origin + firstDirection);
            }
        }

        // Try second diagonal direction
        if (chunk.IsInBounds(origin.X + secondDirection.X, origin.Y + secondDirection.Y))
        {
            PixelElement pixel = chunk.pixels[origin.X + secondDirection.X, origin.Y + secondDirection.Y];
            if (pixel.IsEmpty(this))
            {
                if (!pixel.IsFalling) IsFalling = true;
                Momentum += Mass; // Accumulate momentum based on mass
                return (origin, origin + secondDirection);
            }
        }
        
        // 3. If belowLeft, belowRight and below are all empty Then resolve the momentum
        if (!IsFalling && Momentum > 0)
        {
            // If we haven't set a momentum direction yet (just landed), set it based on last diagonal movement
            if (MomentumDirection == Vector2I.Zero)
            {
                // Use the X component of the last diagonal movement to determine direction
                MomentumDirection = firstDirection.X > 0 ? Vector2I.Right : Vector2I.Left;
            }

            // Move in the stored momentum direction
            Vector2I targetPos = origin + MomentumDirection;
            
            if (chunk.IsInBounds(targetPos.X, targetPos.Y))
            {
                PixelElement pixel = chunk.pixels[targetPos.X, targetPos.Y];
                if (pixel.IsEmpty(this))
                {
                    Momentum--;
                    if (Momentum <= 0)
                    {
                        Momentum = 0;
                        MomentumDirection = Vector2I.Zero; // Reset direction when momentum is used up
                    }
                    return (origin, targetPos);
                }
            }
        }
        else if (IsFalling)
        {
            // We were falling but couldn't move, so we've landed
            IsFalling = false;
            // Don't reset momentum here, it's already accumulated during falling
        }
        
        return (origin, origin);
    }
}
