using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Physics;

/// <summary>
/// Physics behavior for empty space/air pixels
/// Based on the original PixelAir physics logic
/// </summary>
public class EmptyPhysicsBehavior : IPhysicsBehavior
{
    public void UpdatePhysics(PixelElement pixel)
    {
        // Air has no mass and doesn't fall
        pixel.Mass = 0;
        pixel.Friction = 0;
        pixel.IsFalling = false;
        pixel.Momentum = 0;
        pixel.Velocity = Vector2I.Zero;
        pixel.SuddenStop = false;
        pixel.MomentumDirection = Vector2I.Zero;
    }

    public bool ShouldFall(PixelElement pixel)
    {
        // Air doesn't fall
        return false;
    }

    public bool HandleSuddenStop(PixelElement pixel)
    {
        // Air doesn't have sudden stop behavior
        return false;
    }

    public float GetMass()
    {
        return 0;
    }

    public float GetFriction()
    {
        return 0;
    }
}