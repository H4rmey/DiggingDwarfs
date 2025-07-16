using Godot;

namespace SharpDiggingDwarfs.Behaviors;

/// <summary>
/// Physics behavior for granular materials like sand/solid particles
/// Based on the original PixelSolid physics logic
/// </summary>
public class GranularPhysicsBehavior : IPhysicsBehavior
{
    private readonly float mass;
    private readonly float friction;

    public GranularPhysicsBehavior(float mass = 0.33f, float friction = 0.01f)
    {
        this.mass = mass;
        this.friction = friction;
    }

    public void UpdatePhysics(PixelElementComposed pixel)
    {
        // Set the physics properties
        pixel.Mass = mass;
        pixel.Friction = friction;
        
        // Initialize momentum if not set
        if (pixel.Momentum == 0 && pixel.IsFalling)
        {
            pixel.Momentum = mass;
        }
    }

    public bool ShouldFall(PixelElementComposed pixel)
    {
        // Granular materials fall by default unless they've stopped
        return !pixel.SuddenStop;
    }

    public bool HandleSuddenStop(PixelElementComposed pixel)
    {
        if (GD.RandRange(0.0f, 1.0f) < pixel.Friction)
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
}