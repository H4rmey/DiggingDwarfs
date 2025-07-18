using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

/// <summary>
/// Interface for defining how a pixel moves within the simulation
/// </summary>
public interface IMovementBehavior
{
    /// <summary>
    /// Determines the next position for a pixel based on its current position and the chunk state
    /// </summary>
    /// <param name="origin">Current position of the pixel</param>
    /// <param name="chunk">The pixel chunk containing the simulation state</param>
    /// <param name="pixel">The pixel element that is moving</param>
    /// <returns>A tuple containing the current position and the desired next position</returns>
    (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk, PixelElement pixel);
}