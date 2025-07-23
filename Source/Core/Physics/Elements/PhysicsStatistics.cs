using Godot;

namespace SharpDiggingDwarfs.Core.Physics.Elements;

/// <summary>
/// Data container for physics properties of a pixel
/// </summary>
public struct PhysicsStatistics
{
    /// <summary>
    /// Mass of the pixel - affects momentum accumulation and gravity
    /// </summary>
    public float Mass { get; set; }
    
    /// <summary>
    /// Density of the pixel - affects how it interacts with other pixels
    /// </summary>
    public float Density { get; set; }
    
    /// <summary>
    /// Resistance coefficient - affects how easily the pixel stops move (see CancelHorizontalMovement)
    /// </summary>
    public float HorizontalFriction { get; set; }

    /// <summary>
    /// Resistance coefficient - affects how easily the pixel stops move (see CancelVerticalMovement)
    /// </summary>
    public float VerticalFriction { get; set; }
    
    /// <summary>
    /// Whether the pixel will resolve horizontal motion or not
    /// </summary>
    public bool CancelHorizontalMotion { get; set; }

    /// <summary>
    /// Whether the pixel will resolve vertical motion or not
    /// </summary>
    public bool CancelVerticalMotion { get; set; }
    
    /// <summary>
    /// Flow resistance for liquid-like behavior - higher values mean more fluid movement
    /// 
    /// Will determine how many pixels are checked for a new position
    /// </summary>
    public int Viscosity { get; set; }
    
    /// <summary>
    /// Momentum accumulation rate - how quickly momentum builds up when falling
    /// </summary>
    public float MomentumRate { get; set; }
    
    /// <summary>
    /// Threshold for stopping - probability modifier for halt conditions
    /// </summary>
    public float HaltThreshold { get; set; }
    
    // Physics state properties
    private bool _isFalling;
    
    /// <summary>
    /// Whether the pixel is currently falling due to gravity.
    /// When set to true, automatically sets CancelVerticalMotion to true as well
    /// to ensure proper falling behavior.
    /// 
    /// IsFalling is primarily used to obtain wether or not the pixel is falling. 
    /// To actually cancel the falling set CancelVeticalMotion
    /// </summary>
    public bool IsFalling
    {
        get => _isFalling;
        set
        {
            _isFalling = value;
            if (value)
            {
                CancelVerticalMotion = false;
            }
        }
    }
    
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
    
    /// <summary>
    /// Creates physics statistics with default values for empty/air pixels
    /// </summary>
    public static PhysicsStatistics Empty => new PhysicsStatistics
    {
        Mass = 0f,
        Density = 0f,
        HorizontalFriction = 0f,
        VerticalFriction = 0f,
        Viscosity = 0,
        MomentumRate = 0f,
        HaltThreshold = 0f,
        IsFalling = false,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = true,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics statistics for liquid behavior
    /// </summary>
    public static PhysicsStatistics Liquid => new PhysicsStatistics
    {
        Mass = 0.2f,
        Density = 1.0f,
        HorizontalFriction = 0.1f,
        VerticalFriction = 0.1f,
        Viscosity = 8,
        MomentumRate = 0.5f,
        HaltThreshold = 0.05f,
        IsFalling = false,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = true,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics statistics for solid/granular behavior
    /// </summary>
    public static PhysicsStatistics Solid => new PhysicsStatistics
    {
        Mass = 0.33f,
        Density = 2.0f,
        HorizontalFriction = 0.5f,
        VerticalFriction = 0.5f,
        Viscosity = 0,
        MomentumRate = 1.0f,
        HaltThreshold = 0.5f,
        IsFalling = false,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = true,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
    
    /// <summary>
    /// Creates physics statistics for immovable structure behavior
    /// </summary>
    public static PhysicsStatistics Structure => new PhysicsStatistics
    {
        Mass = 10.0f,
        Density = 5.0f,
        HorizontalFriction = 1.0f,
        VerticalFriction = 1.0f,
        Viscosity = 0,
        MomentumRate = 0f,
        HaltThreshold = 1.0f,
        IsFalling = false,
        Velocity = Vector2I.Zero,
        CancelHorizontalMotion = true,
        CancelVerticalMotion = true,
        Momentum = 0f,
        MomentumDirection = Vector2I.Zero
    };
}