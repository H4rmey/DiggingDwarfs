using Godot;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;
using SharpDiggingDwarfs.Core.Physics.Behaviors;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Visual;
using SharpDiggingDwarfs.Core.Physics.Elements;

namespace SharpDiggingDwarfs.Core.Physics.Factory;

/// <summary>
/// Factory for creating different types of composed pixels with appropriate behaviors
/// This demonstrates the new unified behavior pattern in action
/// </summary>
public static class PixelFactory
{
    /// <summary>
    /// Creates an air pixel (empty space)
    /// </summary>
    public static PixelElement CreateAir()
    {
        var pixel = new PixelElement
        {
            Type = PixelType.Empty,
            Behaviour = new EmptyBehaviour(),
            VisualBehavior = new AirVisualBehavior()
        };
        
        // Initialize physics using the unified behavior
        pixel.Behaviour.InitializePhysics(pixel);
        pixel.Behaviour.UpdatePhysics(pixel);
        
        // Initialize visual properties
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    /// <summary>
    /// Creates a solid pixel that falls and accumulates momentum
    /// </summary>
    public static PixelElement CreateSolid()
    {
        var pixel = new PixelElement
        {
            Type = PixelType.Solid,
            Behaviour = new SolidBehaviour(),
            VisualBehavior = new SolidVisualBehavior()
        };
        
        // Initialize physics using the unified behavior
        pixel.Behaviour.InitializePhysics(pixel);
        pixel.Behaviour.UpdatePhysics(pixel);
        
        // Initialize visual properties
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    /// <summary>
    /// Creates a liquid pixel with flow behavior
    /// </summary>
    public static PixelElement CreateLiquid()
    {
        var pixel = new PixelElement
        {
            Type = PixelType.Liquid,
            Behaviour = new LiquidBehaviour(),
            VisualBehavior = new LiquidVisualBehavior()
        };
        
        // Initialize physics using the unified behavior
        pixel.Behaviour.InitializePhysics(pixel);
        pixel.Behaviour.UpdatePhysics(pixel);
        
        // Initialize visual properties
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }

    /// <summary>
    /// Creates a structure pixel that acts as an immovable barrier
    /// </summary>
    public static PixelElement CreateStructure()
    {
        var pixel = new PixelElement
        {
            Type = PixelType.Solid, // Structures use solid state but with different behavior
            Behaviour = new StructureBehaviour(),
            VisualBehavior = new SolidVisualBehavior() // Reuse solid visual for now
        };
        
        // Initialize physics using the unified behavior
        pixel.Behaviour.InitializePhysics(pixel);
        pixel.Behaviour.UpdatePhysics(pixel);
        
        // Initialize visual properties
        pixel.BaseColor = pixel.VisualBehavior.GetBaseColor();
        pixel.VisualBehavior.SetRandomColor(pixel);
        
        return pixel;
    }
}