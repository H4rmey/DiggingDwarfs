using System;
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
    public bool IsVerticalStable = false;
    public bool IsHorizontalStable = false;
    public bool IsAnchor = false;
    
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

    public (Vector2I Current, Vector2I Next) GetSwapPosition(PixelWorld world, PixelChunk chunk, PixelElement pixel, Vector2I origin)
    {
        origin = chunk.ToWorldPosition(origin);
        Vector2I nextPos = new Vector2I(origin.X, origin.Y + 1);
        
        // Initialize pixel references to null to avoid unassigned variable errors
        PixelElement belowPixel = null;
        
        // 1. Check if you can place a pixel directly below
        if (world.IsInBound(nextPos))
        {
            belowPixel = world.GetPixelElementAt(nextPos);
        }

        // Check if this pixel has a SOLID or out of bounds below it - makes it an Anchor Pixel
        if (belowPixel == null || belowPixel.Type == PixelType.Solid || !world.IsInBound(nextPos))
        {
            IsAnchor = true;
            IsVerticalStable = true;
            return (origin, origin);
        }

        // Check if this pixel lands on top of an anchor pixel or vertically stable pixel
        if (belowPixel != null && belowPixel.Behaviour is ScaffoldingBehaviour belowScaffolding)
        {
            if (belowScaffolding.IsAnchor || belowScaffolding.IsVerticalStable)
            {
                IsVerticalStable = true;
                return (origin, origin);
            }
        }

        if (CheckHorizontalStability(world, chunk, origin, -1))
        {
            IsHorizontalStable = true;
            IsVerticalStable = true;
            return (origin, origin);
        }

        if (CheckHorizontalStability(world, chunk, origin, 1))
        {
            IsHorizontalStable = true;
            IsVerticalStable = true;
            return (origin, origin);
        }

        // Check vertical chain stability
        //if (CheckVerticalStability(origin, chunk))
        //{
        //    IsVerticalStable = true;
        //    return (origin, origin);
        //}

        // Check if we can fall
        if (world.IsInBound(nextPos))
        {
            PixelElement targetPixel = world.GetPixelElementAt(nextPos);
            if (targetPixel != null && targetPixel.IsEmpty(pixel))
            {
                return (origin, nextPos);
            }
        }

        // Can't move anywhere
        return (origin, origin);
    }

    /// <summary>
    /// Checks horizontal stability in a given direction (-1 for left, 1 for right)
    /// </summary>
    /// <param name="origin">Starting position</param>
    /// <param name="chunk">The pixel chunk</param>
    /// <param name="direction">Direction to check (-1 for left, 1 for right)</param>
    /// <returns>Tuple of (foundStable, chainCount)</returns>
    private bool CheckHorizontalStability(PixelWorld world, PixelChunk chunk, Vector2I origin, int direction)
    {
        bool foundStable = false;
        
        for (int i = 1; i <= MaxHorizontalChain; i++)
        {
            int checkX = origin.X + (i * direction);
            
            if (!world.IsInBound(new Vector2I(checkX, origin.Y)))
                break;
                
            PixelElement checkPixel = world.GetPixelElementAt(new Vector2I(checkX, origin.Y));
            
            // Check if the pixel is scaffolding
            if (checkPixel != null && checkPixel.Type == PixelType.Scaffolding && checkPixel.Behaviour is ScaffoldingBehaviour scaffolding)
            {
                // Check if this scaffolding pixel has another scaffolding pixel below it and is vertically stable
                PixelElement belowCheckPixel = null;
                if (world.IsInBound(origin))
                {
                    belowCheckPixel = world.GetPixelElementAt(new Vector2I(checkX, origin.Y + 1));
                }
                
                if (scaffolding.IsVerticalStable &&
                    (belowCheckPixel == null ||
                     belowCheckPixel.Type == PixelType.Scaffolding ||
                     belowCheckPixel.Type == PixelType.Solid))
                {
                    foundStable = true;
                    break;
                }
            }
            else
            {
                // Not scaffolding, stop checking
                break;
            }
        }

        return foundStable;
    }

    /// <summary>
    /// Checks vertical stability by looking down for scaffolding chains and solid support
    /// </summary>
    /// <param name="origin">Starting position</param>
    /// <param name="chunk">The pixel chunk</param>
    /// <returns>True if vertical stability is found</returns>
    private bool CheckVerticalStability(Vector2I origin, PixelChunk chunk)
    {
        for (int i = 1; i <= MaxVerticalChain; i++)
        {
            if (!chunk.IsInBound(new Vector2I(origin.X, origin.Y + i)))
                break;
                
            PixelElement checkPixel = chunk.pixels[origin.X, origin.Y + i];
            
            if (checkPixel != null && checkPixel.Type == PixelType.Scaffolding)
            {
                if (checkPixel.Behaviour is ScaffoldingBehaviour scaffolding && scaffolding.IsAnchor)
                {
                    return true;
                }
            }
            else if (checkPixel != null && checkPixel.Type == PixelType.Solid)
            {
                return true;
            }
            else
            {
                break;
            }
        }
        
        return false;
    }
}