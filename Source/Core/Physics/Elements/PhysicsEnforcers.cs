using Godot;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Physics.Elements;

/// <summary>
/// Logic container for physics enforcement behaviors
/// </summary>
public struct PhysicsEnforcers
{
    /// <summary>
    /// Determines if a pixel should stop based on its statistics and current state
    /// </summary>
    /// <param name="pixel">The pixel to check</param>
    /// <returns>True if the pixel should stop</returns>
    public bool ShouldStop(PixelElement pixel)
    {
        if (pixel.Statistics.HaltThreshold <= 0) return false;
        
        return GD.RandRange(0.0f, 1.0f) < pixel.Statistics.Friction * pixel.Statistics.HaltThreshold;
    }
    
    /// <summary>
    /// Applies momentum accumulation based on mass and momentum rate
    /// </summary>
    /// <param name="pixel">The pixel to update</param>
    public void ApplyMomentum(PixelElement pixel)
    {
        if (pixel.Statistics.MomentumRate > 0 && pixel.Statistics.IsFalling)
        {
            pixel.Statistics = pixel.Statistics with
            {
                Momentum = pixel.Statistics.Momentum + pixel.Statistics.Mass * pixel.Statistics.MomentumRate
            };
        }
    }
    
    /// <summary>
    /// Handles stopping behavior and resets momentum when appropriate
    /// </summary>
    /// <param name="pixel">The pixel to potentially stop</param>
    /// <returns>True if the pixel was stopped</returns>
    public bool EnforceStop(PixelElement pixel)
    {
        if (ShouldStop(pixel))
        {
            pixel.Statistics = pixel.Statistics with
            {
                CancelHorizontalMotion = true,
                Momentum = 0.0f,
                MomentumDirection = Vector2I.Zero
            };
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Applies gravitational effects based on mass
    /// </summary>
    /// <param name="pixel">The pixel to apply gravity to</param>
    public void ApplyGravity(PixelElement pixel)
    {
        if (pixel.Statistics.Mass > 0 && !pixel.Statistics.CancelHorizontalMotion)
        {
            pixel.Statistics = pixel.Statistics with { IsFalling = true };
        }
    }
    
    /// <summary>
    /// Handles flow behavior for liquid-like pixels
    /// </summary>
    /// <param name="pixel">The pixel to apply flow to</param>
    /// <param name="origin">Current position</param>
    /// <param name="chunk">The chunk containing the pixel</param>
    public void ApplyFlow(PixelElement pixel, Vector2I origin, PixelChunk chunk)
    {
        if (pixel.Statistics.FlowResistance > 0)
        {
            // Trigger surrounding pixels to potentially start flowing
            pixel.CheckSurroundingPixels(origin, chunk, (adjacentPixel, pos) => {
                if (GD.RandRange(0.0f, 1.0f) < pixel.Statistics.Friction)
                {
                    adjacentPixel.Statistics = adjacentPixel.Statistics with { CancelHorizontalMotion = false };
                }
            });
        }
    }
    
    /// <summary>
    /// Resets physics state for static/empty pixels
    /// </summary>
    /// <param name="pixel">The pixel to reset</param>
    public void ResetPhysics(PixelElement pixel)
    {
        pixel.Statistics = pixel.Statistics with
        {
            IsFalling = false,
            Momentum = 0,
            Velocity = Vector2I.Zero,
            CancelHorizontalMotion = false,
            MomentumDirection = Vector2I.Zero
        };
    }
    
    /// <summary>
    /// Creates enforcers for empty/air behavior
    /// </summary>
    public static PhysicsEnforcers Empty => new PhysicsEnforcers();
    
    /// <summary>
    /// Creates enforcers for liquid behavior
    /// </summary>
    public static PhysicsEnforcers Liquid => new PhysicsEnforcers();
    
    /// <summary>
    /// Creates enforcers for solid behavior
    /// </summary>
    public static PhysicsEnforcers Solid => new PhysicsEnforcers();
    
    /// <summary>
    /// Creates enforcers for structure behavior
    /// </summary>
    public static PhysicsEnforcers Structure => new PhysicsEnforcers();
}