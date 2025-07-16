using Godot;
using SharpDiggingDwarfs.Behaviors;

namespace SharpDiggingDwarfs;

/// <summary>
/// Factory for creating different types of composed pixels with appropriate behaviors
/// This demonstrates the composition pattern in action
/// </summary>
public static class PixelFactory
{
    /// <summary>
    /// Creates an air pixel (empty space)
    /// </summary>
    public static PixelElementComposed CreateAir()
    {
        var pixel = new PixelElementComposed
        {
            State = PixelState.Empty,
            IsFalling = false,
            MovementBehavior = new StaticMovementBehavior(),
            PhysicsBehavior = new EmptyPhysicsBehavior(),
            VisualBehavior = new AirVisualBehavior()
        };
        
        // Initialize physics and visual properties
        pixel.PhysicsBehavior.UpdatePhysics(pixel);
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    /// <summary>
    /// Creates a solid pixel that falls and accumulates momentum
    /// </summary>
    public static PixelElementComposed CreateSolid()
    {
        var pixel = new PixelElementComposed
        {
            State = PixelState.Solid,
            IsFalling = true,
            Velocity = Vector2I.Zero,
            Momentum = 0,
            MovementBehavior = new FallingMovementBehavior(),
            PhysicsBehavior = new GranularPhysicsBehavior(),
            VisualBehavior = new SolidVisualBehavior()
        };
        
        // Initialize physics and visual properties
        pixel.PhysicsBehavior.UpdatePhysics(pixel);
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    /// <summary>
    /// Creates a liquid pixel (placeholder for future implementation)
    /// </summary>
    public static PixelElementComposed CreateLiquid()
    {
        var pixel = new PixelElementComposed
        {
            State = PixelState.Liquid,
            IsFalling = true,
            Velocity = Vector2I.Zero,
            Momentum = 0,
            MovementBehavior = new LiquidFlowBehavior(),
            PhysicsBehavior = new FluidPhysicsBehavior(),
            VisualBehavior = new LiquidVisualBehavior()
        };
        
        // Initialize physics and visual properties
        pixel.PhysicsBehavior.UpdatePhysics(pixel);
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    // Old conversion method removed - no longer needed since old classes are deleted
}