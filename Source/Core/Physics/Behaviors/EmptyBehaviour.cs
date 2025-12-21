using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for empty/air pixels that combines physics and movement logic
/// Based on the original EmptyPhysicsBehavior and StaticMovementBehavior
/// </summary>
public class EmptyBehaviour : IPixelBehaviour
{
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = new PhysicsHelper
        {
            Mass = 0f,
            Density = 0f,
            HorizontalStability = 0f,
            VerticalStability = 0f,
            Viscosity = 0,
            MomentumRate = 0f,
            HaltThreshold = 0f,
            Velocity = Vector2I.Zero,
            CancelHorizontalMotion = true,
            CancelVerticalMotion = true,
            Momentum = 0f,
            MomentumDirection = Vector2I.Zero
        };
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Apply helper to reset physics state
        pixel.Physics.ResetPhysics(pixel);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Empty pixels don't fall
        return false;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(PixelWorld world, PixelChunk chunk, PixelElement pixel, Vector2I origin)
    {
        // Empty pixels never move
        return (origin, origin);
    }
}