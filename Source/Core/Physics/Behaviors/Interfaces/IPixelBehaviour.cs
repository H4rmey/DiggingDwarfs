using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

/// <summary>
/// Unified interface for pixel behavior that combines physics and movement logic
/// </summary>
public interface IPixelBehaviour
{
    /// <summary>
    /// Updates the physics state of the pixel using its statistics and enforcers
    /// </summary>
    /// <param name="pixel">The pixel to update</param>
    void UpdatePhysics(PixelElement pixel);
    
    /// <summary>
    /// Determines the next position for a pixel based on its current position and the chunk state
    /// </summary>
    /// <param name="origin">Current position of the pixel</param>
    /// <param name="chunk">The pixel chunk containing the simulation state</param>
    /// <param name="pixel">The pixel element that is moving</param>
    /// <returns>A tuple containing the current position and the desired next position</returns>
    (Vector2I Current, Vector2I Next) GetSwapPosition(PixelWorld pixelWorld, PixelChunk pixelChunk, PixelElement pixel, Vector2I origin);
    
    /// <summary>
    /// Determines if the pixel should be falling based on its current state and enforcers
    /// </summary>
    /// <param name="pixel">The pixel to check</param>
    /// <returns>True if the pixel should be falling</returns>
    bool ShouldFall(PixelElement pixel);
    
    /// <summary>
    /// Initializes the physics statistics and enforcers for this behavior type
    /// </summary>
    /// <param name="pixel">The pixel to initialize</param>
    void InitializePhysics(PixelElement pixel);
}