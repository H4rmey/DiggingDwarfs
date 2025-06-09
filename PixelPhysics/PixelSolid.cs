using System.Collections.Generic;
using Godot;

namespace SharpDiggingDwarfs;
public class PixelSolid : PixelElement
{
    private Vector2I momentumDirection = Vector2I.Zero; // Store the direction of momentum
    private bool suddenStop = false;

    public PixelSolid()
    {
        BaseColor = Colors.Yellow; 
        Color     = Colors.Orange; 
        State     = PixelState.Solid; 
        IsFalling = true;
        Mass      = 0.33f;
        Velocity  = Vector2I.Zero;
        Momentum  = 0; // Start with no momentum
        Friction  = 0.001f; // Start with no momentum
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
                return (origin, new Vector2I(origin.X, origin.Y + 1));
            }
        }
        
        if (suddenStop) return (origin, origin);
        // 1.1 if we cannot go down directly stop falling or check left/right/momentum
        if (GD.RandRange(0.0f, 1.0f) < Friction)
        {
            suddenStop = true;
            momentumDirection = Vector2I.Zero; // Reset direction when momentum is used up
            return (origin, origin);
        }
        
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
            if (momentumDirection == Vector2I.Zero)
            {
                // Use the X component of the last diagonal movement to determine direction
                momentumDirection = firstDirection.X > 0 ? Vector2I.Right : Vector2I.Left;
            }

            // Move in the stored momentum direction
            Vector2I targetPos = origin + momentumDirection;
            
            if (chunk.IsInBounds(targetPos.X, targetPos.Y))
            {
                PixelElement pixel = chunk.pixels[targetPos.X, targetPos.Y];
                if (pixel.IsEmpty(this))
                {
                    Momentum--;
                    if (Momentum <= 0)
                    {
                        Momentum = 0;
                        momentumDirection = Vector2I.Zero; // Reset direction when momentum is used up
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
