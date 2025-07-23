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
        pixel.Statistics = PhysicsStatistics.Empty;
        pixel.Enforcers = PhysicsEnforcers.Empty;
    }

    public void UpdatePhysics(PixelElement pixel)
    {
        // Apply enforcers to reset physics state
        pixel.Enforcers.ResetPhysics(pixel);
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Empty pixels don't fall
        return false;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // Empty pixels never move
        return (origin, origin);
    }
}