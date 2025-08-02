using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for structure pixels that act as immovable barriers
/// These pixels have high mass and density but never move, and provide stability to surrounding solid pixels
/// </summary>
public class ScaffoldingBehaviour : IPixelBehaviour
{
    public int MaxSize = 10;
    
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Structure;
        pixel.Type = PixelType.Structure;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Structures are always static - force stop all movement
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
        // Solids fall unless they've been stopped by physics
        return !pixel.Physics.CancelHorizontalMotion && pixel.Physics.Mass > 0;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // if a pixel is falling, make sure the vertical motion is allowed.
        if (pixel.Physics.IsFalling) pixel.Physics = pixel.Physics with { CancelVerticalMotion = false };
        
        if (pixel.Physics.CancelVerticalMotion)                                           return (origin, origin);
        //if (pixel.Physics.DoCancelVerticalMotion(pixel, pixel.Physics.VerticalStability)) return (origin, origin);


        // 1. Check if you can place a pixel directly below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            PixelElement belowPixel = chunk.pixels[origin.X, origin.Y + 1];
            if (belowPixel.IsEmpty(pixel))
            {
                pixel.Physics = pixel.Physics with { IsFalling = true, CancelVerticalMotion = false };

                // The pixel below the current pixel should be falling, as it carries momentum so that momentum is transfered to the pixel below it.
                // TODO: make it so the IsFalling is only set based on a calculation with mass, momentum and friction.
                chunk.pixels[origin.X, origin.Y + 1].Physics = chunk.pixels[origin.X, origin.Y + 1].Physics with { IsFalling = true, CancelVerticalMotion = false };

                // when a pixel falls down next to a another pixel it has a chance to "drag" the other pixel with it
                pixel.CheckSurroundingPixels(origin, chunk, (adjacentPixel, pos) =>
                {
                    if (GD.RandRange(0.0f, 1.0f) < pixel.Physics.HorizontalStability)
                    {
                        adjacentPixel.Physics = adjacentPixel.Physics with { CancelHorizontalMotion = false, CancelVerticalMotion = false };
                    }
                });

                // finally apply the new momentum 
                pixel.Physics.ApplyMomentum(pixel);
                return (origin, new Vector2I(origin.X, origin.Y + 1));
            }
        }
        
        // Structure pixels never move - they act as barriers
        return (origin, origin);
    }
}