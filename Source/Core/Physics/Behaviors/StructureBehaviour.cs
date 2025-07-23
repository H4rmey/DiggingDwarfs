using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors;

/// <summary>
/// Unified behavior for structure pixels that act as immovable barriers
/// These pixels have high mass and density but never move
/// </summary>
public class StructureBehaviour : IPixelBehaviour
{
    public void InitializePhysics(PixelElement pixel)
    {
        pixel.Physics = PhysicsHelper.Structure;
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
        // Structure pixels never fall - they are immovable
        return false;
    }

    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // Structure pixels never move - they act as barriers
        return (origin, origin);
    }
}