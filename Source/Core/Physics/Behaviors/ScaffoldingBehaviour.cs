using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for scaffolding pixels that act as immovable barriers
/// These pixels have high mass and density but never move, and provide stability to surrounding solid pixels
/// </summary>
public class ScaffoldingBehaviour : IPixelBehaviour
{
    public int MaxVerticalChain = 10;
    public int MaxHorizontalChain = 5;
    public int CurrentVerticalChain = 0;
    public int CurrentHorizontalChain = 0;
    public bool IsVerticalStable = false;
    public bool IsHorizontalStable = false;
    
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Scaffolding;
        pixel.Type = PixelType.Scaffolding;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Scaffolding physics are now handled in GetSwapPosition
        // Don't override the physics state here - let the stability system control it
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Scaffolding can fall if it's not stable
        return !IsVerticalStable || !IsHorizontalStable;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // Initialize pixel references to null to avoid unassigned variable errors
        PixelElement belowPixel = null;
        PixelElement topPixel = null;
        PixelElement leftPixel = null;
        PixelElement rightPixel = null;

        // Get neighboring pixels within chunk bounds for stability calculations
        if (chunk.IsInBounds(origin.X, origin.Y + 1)) { belowPixel = chunk.pixels[origin.X, origin.Y + 1]; }
        if (chunk.IsInBounds(origin.X, origin.Y - 1)) { topPixel = chunk.pixels[origin.X, origin.Y - 1]; }
        if (chunk.IsInBounds(origin.X - 1, origin.Y)) { leftPixel = chunk.pixels[origin.X - 1, origin.Y]; }
        if (chunk.IsInBounds(origin.X + 1, origin.Y)) { rightPixel = chunk.pixels[origin.X + 1, origin.Y]; }

        // Move one down if none are stable
        if (belowPixel != null && belowPixel.IsEmpty(pixel) && CurrentHorizontalChain > MaxHorizontalChain)
        {
            return (origin, new Vector2I(origin.X, origin.Y + 1));
        }

        if (pixel.Physics.CancelHorizontalMotion) { return (origin, origin);  }

        // Check if left is stable
        // If left is stable in both directions set this to stable and return
        if (leftPixel != null && IsNeighborStable(leftPixel))
        {
            SetStableState(pixel);
            IncreaseHorizontalChain(pixel, leftPixel);
            pixel.Physics = pixel.Physics with { CancelHorizontalMotion = true };
            return (origin, origin);
        }

        // Check if right is stable
        // If right is stable in both directions set this to stable and return
        if (rightPixel != null && IsNeighborStable(rightPixel))
        {
            SetStableState(pixel);
            IncreaseHorizontalChain(pixel, rightPixel);
            pixel.Physics = pixel.Physics with { CancelHorizontalMotion = true };
            return (origin, origin);
        }

        // Check if below is stable
        // If below is stable set this to stable and return
        if (belowPixel != null && IsNeighborStable(belowPixel))
        {
            SetStableState(pixel);
            IncreaseVerticalChain(pixel, rightPixel);
            return (origin, origin);
        }

        // Reach the ground if moving out of bounds on the below or if you see a solid then set to stable
        if (!chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            // Hit bottom boundary - this is stable ground
            SetStableState(pixel);
            return (origin, origin);
        }

        if (belowPixel != null && belowPixel.Type == PixelType.Solid)
        {
            // Standing on solid ground - this is stable
            SetStableState(pixel);
            return (origin, origin);
        }

        // Move one down if none are stable
        if (belowPixel != null && belowPixel.IsEmpty(pixel))
        {
            if (topPixel != null && topPixel.Behaviour is ScaffoldingBehaviour topBehaviour)
            {
                topPixel.Physics = topPixel.Physics with { CancelHorizontalMotion = false };
            }

            SetFallingState(pixel);


            return (origin, new Vector2I(origin.X, origin.Y + 1));
        }

        // If can't move down but no stable support, stay in place but mark as unstable
        SetFallingState(pixel);
        return (origin, origin);
    }

    private void IncreaseVerticalChain(PixelElement pixel, PixelElement otherPixel)
    {
        if (otherPixel.Behaviour is ScaffoldingBehaviour scaffoldBehaviour)
        {
            CurrentVerticalChain += scaffoldBehaviour.CurrentVerticalChain + 1;
        }
    }

    private void IncreaseHorizontalChain(PixelElement pixel, PixelElement otherPixel)
    {
        if (otherPixel.Behaviour is ScaffoldingBehaviour scaffoldBehaviour)
        {
            CurrentHorizontalChain += scaffoldBehaviour.CurrentHorizontalChain + 1;
        }
    }

    /// <summary>
    /// Checks if a neighbor pixel is stable (has both vertical and horizontal stability)
    /// </summary>
    private bool IsNeighborStable(PixelElement neighborPixel)
    {
        if (neighborPixel == null) return false;

        // Solid pixels are always stable
        if (neighborPixel.Type == PixelType.Solid) return true;

        // Check if it's scaffolding with stability
        if (neighborPixel.Behaviour is ScaffoldingBehaviour scaffoldBehaviour)
        {
            if (scaffoldBehaviour.CurrentHorizontalChain + 1 > MaxHorizontalChain) return false;
            if (scaffoldBehaviour.CurrentVerticalChain + 1 > MaxVerticalChain) return false;

            return scaffoldBehaviour.IsVerticalStable && scaffoldBehaviour.IsHorizontalStable;
        }

        return false;
    }

    /// <summary>
    /// Sets the pixel to a stable state (not falling, stationary)
    /// </summary>
    private void SetStableState(PixelElement pixel)
    {
        IsVerticalStable = true;
        IsHorizontalStable = true;

        pixel.Physics = pixel.Physics with
        {
            IsFalling = false,
            CancelVerticalMotion = true,
            CancelHorizontalMotion = false,
            Momentum = 0,
            Velocity = Vector2I.Zero,
            MomentumDirection = Vector2I.Zero
        };
    }

    /// <summary>
    /// Sets the pixel to a falling state (can move, affected by gravity)
    /// </summary>
    private void SetFallingState(PixelElement pixel)
    {
        IsVerticalStable = false;
        IsHorizontalStable = false;
        
        pixel.Physics = pixel.Physics with
        {
            IsFalling = true,
            CancelVerticalMotion = false,
            CancelHorizontalMotion = false
        };
    }
}