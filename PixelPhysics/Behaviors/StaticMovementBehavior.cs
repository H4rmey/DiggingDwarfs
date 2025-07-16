using Godot;

namespace SharpDiggingDwarfs.Behaviors;

/// <summary>
/// Movement behavior for static pixels that don't move (like air)
/// Based on the original PixelAir movement logic
/// </summary>
public class StaticMovementBehavior : IMovementBehavior
{
    public (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElementComposed pixel)
    {
        // Static pixels never move
        return (origin, origin);
    }
}