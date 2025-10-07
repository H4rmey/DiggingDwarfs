using Godot;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Physics.Elements;

/// <summary>
/// Combined physics helper class for pixel elements - handles both data and logic.
/// This class replaces and combines the functionality of PhysicsStatistics and PhysicsEnforcers.
/// </summary>
public class PhysicsHelper
{
    #region Properties from PhysicsStatistics

    /// <summary>
    /// Mass of the pixel - affects momentum accumulation and gravity
    /// </summary>
    public float Mass { get; set; }
    
    /// <summary>
    /// Density of the pixel - affects how it interacts with other pixels
    /// </summary>
    public float Density { get; set; }
    
    /// <summary>
    /// Resistance coefficient - affects how easily the pixel stops move
    /// </summary>
    public float HorizontalStability { get; set; }

    /// <summary>
    /// Resistance coefficient - affects how easily the pixel stops move
    /// </summary>
    public float VerticalStability { get; set; }

    public float TotalHorizontalStability { get; set; }

    public float TotalVerticalStability { get; set; }
    
    /// <summary>
    /// Whether the pixel will resolve horizontal motion or not
    /// </summary>
    public bool CancelHorizontalMotion { get; set; }

    /// <summary>
    /// Whether the pixel will resolve vertical motion or not
    /// </summary>
    public bool CancelVerticalMotion { get; set; }
    
    /// <summary>
    /// Sets the stability based on the pixel around it, only used by Solid/Scaffolding pixels
    ///     on Scaffolding pixels, we set a stability value. The bigger the scaffolding the better the stability (some other rules apply)
    ///     on Solid pixels, we apply the stability by adding it to Vertical/Horizontal Stability
    /// </summary>
    public float Stability { get; set; }

    public float TotalStability { get; set; }
    
    /// <summary>
    /// Flow resistance for liquid-like behavior - higher values mean more fluid movement
    /// 
    /// Will determine how many pixels are checked for a new position
    /// </summary>
    public int Viscosity { get; set; }
    
    /// <summary>
    /// [Deprecated] Momentum accumulation rate - no longer used
    /// Momentum is now calculated using Mass and Velocity in the ApplyMomentum method
    /// </summary>
    public float MomentumRate { get; set; }
    
    /// <summary>
    /// Threshold for stopping - probability modifier for halt conditions
    /// </summary>
    public float HaltThreshold { get; set; }
    
    // Physics state properties
    //private bool _isFalling;
    
    ///// <summary>
    ///// Whether the pixel is currently falling due to gravity.
    ///// When set to true, automatically sets CancelVerticalMotion to true as well
    ///// to ensure proper falling behavior.
    ///// 
    ///// IsFalling is primarily used to obtain wether or not the pixel is falling. 
    ///// To actually cancel the falling set CancelVeticalMotion
    ///// </summary>
    //public bool IsFalling
    //{
    //    get => _isFalling;
    //    set
    //    {
    //        _isFalling = value;
    //        if (value)
    //        {
    //            CancelVerticalMotion = false;
    //        }
    //    }
    //}
    
    /// <summary>
    /// Current velocity vector of the pixel
    /// </summary>
    public Vector2I Velocity { get; set; }
    
    /// <summary>
    /// Current momentum value of the pixel
    /// </summary>
    public float Momentum { get; set; }
    
    /// <summary>
    /// Direction of momentum movement
    /// </summary>
    public Vector2I MomentumDirection { get; set; }

    #endregion

    #region Methods from PhysicsEnforcers

    /// <summary>
    /// Determines if a pixel should stop based on its statistics and current state
    /// </summary>
    /// <param name="pixel">The pixel to check</param>
    /// <returns>True if the pixel should stop</returns>
    public bool ShouldStop(PixelElement pixel)
    {
        if (this.HaltThreshold <= 0) return false;

        return GD.RandRange(0.0f, 1.0f) < this.HorizontalStability;
    }
    
    /// <summary>
    /// Applies momentum calculation based on mass and velocity
    /// </summary>
    /// <param name="pixel">The pixel to update</param>
    public void ApplyMomentum(PixelElement pixel)
    {
        if (! CancelVerticalMotion)
        {
            Momentum = Momentum + (Mass * MomentumRate);
        }
    }
    
    public bool DoCancelVerticalMotion()
    {
        if (GD.RandRange(0.0f, 1.0f) < VerticalStability)
        {
            CancelVerticalMotion = true;
            Momentum = 0.0f;
            MomentumDirection = Vector2I.Zero;
            return true;
        }

        CancelVerticalMotion = false;
        
        return false;
    }

    public bool DoCancelHorizontalMotion(PixelElement pixel, float stability)
    {
        if (GD.RandRange(0.0f, 1.0f) < stability)
        {
            CancelHorizontalMotion = true;
            Momentum = 0.0f;
            MomentumDirection = Vector2I.Zero;
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
        if (this.Mass > 0 && !this.CancelHorizontalMotion)
        {
            CancelVerticalMotion = false;
        }
    }
    
    /// <summary>
    /// Handles flow behavior for liquid-like pixels
    /// </summary>
    /// <param name="pixel">The pixel to apply flow to</param>
    /// <param name="origin">Current position</param>
    /// <param name="chunk">The chunk containing the pixel</param>
    public void ApplyFlow(PixelWorld world, PixelChunk chunk, PixelElement pixel, Vector2I origin)
    {
        if (this.Viscosity > 0)
        {
            // Copy the value to use inside the lambda to avoid 'this' capture in struct
            float horizontalFriction = this.HorizontalStability;
            
            // Trigger surrounding pixels to potentially start flowing
            pixel.ExecuteSurroundingPixel(world, chunk, origin, (adjacentPixel, pos) => {
                if (GD.RandRange(0.0f, 1.0f) < horizontalFriction)
                {
                    CancelHorizontalMotion = false;
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
        Momentum = 0;
        Velocity = Vector2I.Zero;
        CancelHorizontalMotion = false;
        CancelVerticalMotion = false;
        MomentumDirection = Vector2I.Zero;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates physics helper with default values for empty/air pixels
    /// </summary>
    public static PhysicsHelper Empty => new PhysicsHelper
    {
        Mass = 0f,
        Density = 0f,
        HorizontalStability = 0f,
        VerticalStability = 0f,
        Viscosity = 0,
        MomentumRate = 0f,
        HaltThreshold = 0f,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = true,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics helper for liquid behavior
    /// </summary>
    public static PhysicsHelper Liquid => new PhysicsHelper
    {
        Mass = 0.2f,
        Density = 1.0f,
        HorizontalStability = 0.5f,
        VerticalStability = 0.1f,
        Viscosity = 8,
        MomentumRate = 0.5f,
        HaltThreshold = 0.05f,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = false,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics helper for solid/granular behavior
    /// </summary>
    public static PhysicsHelper Solid => new PhysicsHelper
    {
        Mass = 0.33f,
        Density = 2.0f,
        HorizontalStability = 0.25f,
        VerticalStability = 0.75f,
        Stability = 1.0f,
        Viscosity = 0,
        MomentumRate = 1.0f,
        HaltThreshold = 0.5f,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = false,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics helper for immovable scaffolding behavior
    /// </summary>
    public static PhysicsHelper Scaffolding => new PhysicsHelper
    {
        Mass = 10.0f,
        Density = 5.0f,
        HorizontalStability = 1.0f,
        VerticalStability = 1.0f,
        Viscosity = 0,
        MomentumRate = 0f,
        HaltThreshold = 1.0f,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = false,
        CancelVerticalMotion = false,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };

    #endregion
}