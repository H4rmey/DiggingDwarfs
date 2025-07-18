using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Movement;

/// <summary>
/// Movement behavior for static pixels that don't move (like air)
/// Based on the original PixelAir movement logic
/// </summary>
public class StaticMovementBehavior : IMovementBehavior
{
    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel)
    {
        // Static pixels never move
        return (origin, origin);
    }
}