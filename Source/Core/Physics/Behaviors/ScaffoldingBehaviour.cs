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
    public int MaxVerticalCount = 10;
    public int MaxHorizontalCount = 5;
    public int CurrentVerticalCount = 0;
    public int CurrentHorizontalCount = 0;

    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Scaffolding;
        pixel.Type = PixelType.Scaffolding;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Scaffoldings are always static - force stop all movement
        pixel.Physics = pixel.Physics with
        {
            IsFalling = false,
            Momentum = 0,
            Velocity = Vector2I.Zero,
            CancelHorizontalMotion = true,
            MomentumDirection = Vector2I.Zero
        };
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Scaffolding pixels never fall - they act as immovable barriers
        return false;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // if a pixel is falling, make sure the vertical motion is allowed.
        if (pixel.Physics.IsFalling) pixel.Physics = pixel.Physics with { CancelVerticalMotion = false };

        if (pixel.Physics.CancelVerticalMotion) return (origin, origin);

        // Initialize pixel references to null/default to avoid unassigned variable errors
        PixelElement belowPixel = null;
        PixelElement leftPixel = null;
        PixelElement rightPixel = null;

        // 1. Check if you can place a pixel directly below
        if (chunk.IsInBounds(origin.X, origin.Y + 1)) { belowPixel = chunk.pixels[origin.X, origin.Y + 1]; }
        if (chunk.IsInBounds(origin.X - 1, origin.Y)) { leftPixel = chunk.pixels[origin.X - 1, origin.Y]; }
        if (chunk.IsInBounds(origin.X + 1, origin.Y)) { rightPixel = chunk.pixels[origin.X + 1, origin.Y]; }

        if (rightPixel != null && rightPixel.Behaviour is ScaffoldingBehaviour)
        {
            GD.Print("LeftPixelFound!");
            SetNewVerticalStability(pixel, rightPixel);
            if (rightPixel.Behaviour is ScaffoldingBehaviour scaffoldingRight)
            {
                CurrentHorizontalCount = scaffoldingRight.CurrentHorizontalCount + 1;
                if (CurrentHorizontalCount > MaxHorizontalCount)
                {
                    CurrentHorizontalCount = 0;
                }
                else
                {
                    pixel.Physics = pixel.Physics with { IsFalling = false, CancelVerticalMotion = true };
                    return (origin, origin);
                }
            }
        }

        if (leftPixel != null && leftPixel.Behaviour is ScaffoldingBehaviour)
        {
            GD.Print("RightPixelFound!");
            SetNewVerticalStability(pixel, leftPixel);
            if (leftPixel.Behaviour is ScaffoldingBehaviour scaffoldingLeft)
            {
                CurrentHorizontalCount = scaffoldingLeft.CurrentHorizontalCount + 1;
                GD.Print("RightPixelFound: " + CurrentHorizontalCount);
                if (CurrentHorizontalCount > MaxHorizontalCount)
                {
                    GD.Print("RightPixelFoundReset");
                    CurrentHorizontalCount = 0;
                }
                else
                {
                    pixel.Physics = pixel.Physics with { IsFalling = false, CancelVerticalMotion = true };
                    return (origin, origin);
                }
            }
        }

        if (belowPixel == null)
        {
            pixel.Physics = pixel.Physics with { TotalVerticalStability = pixel.Physics.VerticalStability };
            return (origin, origin);
        }

        if (belowPixel.IsEmpty(pixel))
        {
            GD.Print("GoGoBelow!");
            return (origin, new Vector2I(origin.X, origin.Y + 1));
        }

        //pixel.Physics = pixel.Physics with { TotalVerticalStability = pixel.Physics.VerticalStability + belowPixel.Physics.TotalVerticalStability };

        //if (belowPixel.Behaviour is ScaffoldingBehaviour scaffoldingBelow)
        //{
        //    GD.Print("HorizontalCountSet!");
        //    CurrentVerticalCount = scaffoldingBelow.CurrentVerticalCount + 1;
        //}

        // Scaffolding pixels never move - they act as barriers
        return (origin, origin);
    }

    private void SetNewVerticalStability(PixelElement pixel, PixelElement otherPixel)
    {
        pixel.Physics = pixel.Physics with { TotalVerticalStability = pixel.Physics.VerticalStability + otherPixel.Physics.TotalVerticalStability };
    }

    private void SetNewHorizontalStability(PixelElement pixel, PixelElement otherPixel)
    {
        pixel.Physics = pixel.Physics with { TotalHorizontalStability = pixel.Physics.HorizontalStability + otherPixel.Physics.TotalHorizontalStability };
    }
}