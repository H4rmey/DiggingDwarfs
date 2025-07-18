using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

/// <summary>
/// Interface for defining physics properties and behaviors of pixels
/// </summary>
public interface IPhysicsBehavior
{
    /// <summary>
    /// Updates the physics state of the pixel (momentum, friction, etc.)
    /// </summary>
    /// <param name="pixel">The pixel to update</param>
    void UpdatePhysics(PixelElement pixel);
    
    /// <summary>
    /// Determines if the pixel should be falling based on its current state
    /// </summary>
    /// <param name="pixel">The pixel to check</param>
    /// <returns>True if the pixel should be falling</returns>
    bool ShouldFall(PixelElement pixel);
    
    /// <summary>
    /// Handles sudden stop behavior based on friction
    /// </summary>
    /// <param name="pixel">The pixel to check</param>
    /// <returns>True if the pixel should suddenly stop</returns>
    bool HandleSuddenStop(PixelElement pixel);
    
    /// <summary>
    /// Gets the mass of the pixel for physics calculations
    /// </summary>
    /// <returns>The mass value</returns>
    float GetMass();
    
    /// <summary>
    /// Gets the friction coefficient for the pixel
    /// </summary>
    /// <returns>The friction value</returns>
    float GetFriction();
}