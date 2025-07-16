using Godot;

namespace SharpDiggingDwarfs.Behaviors;

/// <summary>
/// Physics behavior for fluid materials like water/liquid particles
/// Based on the original PixelLiquid physics logic
/// </summary>
public class FluidPhysicsBehavior : IPhysicsBehavior
{
    private readonly float mass;
    private readonly float friction;
    private readonly float viscosity;

    public FluidPhysicsBehavior(float mass = 0.2f, float friction = 0.05f, float viscosity = 8.0f)
    {
        this.mass = mass;
        this.friction = friction;
        this.viscosity = viscosity;
    }

    public void UpdatePhysics(PixelElementComposed pixel)
    {
        // Set the physics properties
        pixel.Mass = mass;
        pixel.Friction = friction;
        
        // Liquids have less momentum accumulation than solids
        if (pixel.Momentum == 0 && pixel.IsFalling)
        {
            pixel.Momentum = mass * 0.5f; // Less momentum than solids
        }
    }

    public bool ShouldFall(PixelElementComposed pixel)
    {
        // Liquids fall unless they've settled
        return !pixel.SuddenStop;
    }

    public bool HandleSuddenStop(PixelElementComposed pixel)
    {
        // Liquids are less likely to suddenly stop due to lower friction
        if (GD.RandRange(0.0f, 1.0f) < pixel.Friction * 0.5f)
        {
            pixel.SuddenStop = true;
            pixel.Momentum = 0.0f;
            pixel.MomentumDirection = Vector2I.Zero;
            return true;
        }
        return false;
    }

    public float GetMass()
    {
        return mass;
    }

    public float GetFriction()
    {
        return friction;
    }

    public float GetViscosity()
    {
        return viscosity;
    }
}