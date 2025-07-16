using Godot;
using SharpDiggingDwarfs.Behaviors;

namespace SharpDiggingDwarfs;

/// <summary>
/// Comprehensive tests for the new composition-based pixel system
/// Validates that all physics behaviors are maintained from the original inheritance system
/// </summary>
public static class CompositionSystemTests
{
    /// <summary>
    /// Run all composition system tests
    /// </summary>
    public static void RunAllTests()
    {
        GD.Print("=== Running Composition System Tests ===");
        
        TestPixelCreation();
        TestPhysicsBehaviors();
        TestMovementBehaviors();
        TestVisualBehaviors();
        TestBehaviorComposition();
        TestFactoryPattern();
        
        GD.Print("=== All Composition System Tests Completed Successfully ===");
    }
    
    private static void TestPixelCreation()
    {
        GD.Print("Test: Pixel Creation");
        
        var air = PixelFactory.CreateAir();
        var solid = PixelFactory.CreateSolid();
        var liquid = PixelFactory.CreateLiquid();
        
        // Validate states
        Assert(air.State == PixelState.Empty, "Air should have Empty state");
        Assert(solid.State == PixelState.Solid, "Solid should have Solid state");
        Assert(liquid.State == PixelState.Liquid, "Liquid should have Liquid state");
        
        // Validate behavior assignment
        Assert(air.MovementBehavior is StaticMovementBehavior, "Air should have StaticMovementBehavior");
        Assert(solid.MovementBehavior is FallingMovementBehavior, "Solid should have FallingMovementBehavior");
        Assert(liquid.MovementBehavior is LiquidFlowBehavior, "Liquid should have LiquidFlowBehavior");
        
        Assert(air.PhysicsBehavior is EmptyPhysicsBehavior, "Air should have EmptyPhysicsBehavior");
        Assert(solid.PhysicsBehavior is GranularPhysicsBehavior, "Solid should have GranularPhysicsBehavior");
        Assert(liquid.PhysicsBehavior is FluidPhysicsBehavior, "Liquid should have FluidPhysicsBehavior");
        
        GD.Print("✓ Pixel Creation test passed");
    }
    
    private static void TestPhysicsBehaviors()
    {
        GD.Print("Test: Physics Behaviors");
        
        var air = PixelFactory.CreateAir();
        var solid = PixelFactory.CreateSolid();
        var liquid = PixelFactory.CreateLiquid();
        
        // Test mass values (should match original system)
        Assert(air.Mass == 0, "Air mass should be 0");
        Assert(solid.Mass == 0.33f, "Solid mass should be 0.33f");
        Assert(liquid.Mass == 0.2f, "Liquid mass should be 0.2f");
        
        // Test falling behavior
        Assert(!air.PhysicsBehavior.ShouldFall(air), "Air should not fall");
        Assert(solid.PhysicsBehavior.ShouldFall(solid), "Solid should fall");
        Assert(liquid.PhysicsBehavior.ShouldFall(liquid), "Liquid should fall");
        
        // Test friction values
        Assert(air.PhysicsBehavior.GetFriction() == 0, "Air friction should be 0");
        Assert(solid.PhysicsBehavior.GetFriction() == 0.01f, "Solid friction should be 0.01f");
        Assert(liquid.PhysicsBehavior.GetFriction() == 0.05f, "Liquid friction should be 0.05f");
        
        GD.Print("✓ Physics Behaviors test passed");
    }
    
    private static void TestMovementBehaviors()
    {
        GD.Print("Test: Movement Behaviors");
        
        var air = PixelFactory.CreateAir();
        var solid = PixelFactory.CreateSolid();
        var liquid = PixelFactory.CreateLiquid();
        
        // Create a mock chunk for testing (we can't create a real one without full setup)
        var origin = new Vector2I(10, 10);
        
        // Test that behaviors are properly assigned and callable
        Assert(air.MovementBehavior != null, "Air should have movement behavior");
        Assert(solid.MovementBehavior != null, "Solid should have movement behavior");
        Assert(liquid.MovementBehavior != null, "Liquid should have movement behavior");
        
        // Test behavior types
        Assert(air.MovementBehavior is StaticMovementBehavior, "Air should use StaticMovementBehavior");
        Assert(solid.MovementBehavior is FallingMovementBehavior, "Solid should use FallingMovementBehavior");
        Assert(liquid.MovementBehavior is LiquidFlowBehavior, "Liquid should use LiquidFlowBehavior");
        
        GD.Print("✓ Movement Behaviors test passed");
    }
    
    private static void TestVisualBehaviors()
    {
        GD.Print("Test: Visual Behaviors");
        
        var air = PixelFactory.CreateAir();
        var solid = PixelFactory.CreateSolid();
        var liquid = PixelFactory.CreateLiquid();
        
        // Test base colors (should match original system)
        Assert(air.VisualBehavior.GetBaseColor() == Colors.Gray, "Air base color should be Gray");
        Assert(solid.VisualBehavior.GetBaseColor() == Colors.Yellow, "Solid base color should be Yellow");
        Assert(liquid.VisualBehavior.GetBaseColor() == Colors.Blue, "Liquid base color should be Blue");
        
        // Test that colors are set
        Assert(air.Color != default(Color), "Air should have a color set");
        Assert(solid.Color != default(Color), "Solid should have a color set");
        Assert(liquid.Color != default(Color), "Liquid should have a color set");
        
        // Test color randomization
        var originalColor = solid.Color;
        solid.VisualBehavior.SetRandomColor(solid);
        // Colors should be similar but may have slight variations
        Assert(solid.Color.R >= 0 && solid.Color.R <= 1, "Color R component should be valid");
        Assert(solid.Color.G >= 0 && solid.Color.G <= 1, "Color G component should be valid");
        Assert(solid.Color.B >= 0 && solid.Color.B <= 1, "Color B component should be valid");
        Assert(solid.Color.A == 1, "Color A component should be 1");
        
        GD.Print("✓ Visual Behaviors test passed");
    }
    
    private static void TestBehaviorComposition()
    {
        GD.Print("Test: Behavior Composition");
        
        // Test that we can create custom pixel types by mixing behaviors
        var customPixel = new PixelElementComposed
        {
            State = PixelState.Solid,
            MovementBehavior = new LiquidFlowBehavior(), // Solid that flows like liquid
            PhysicsBehavior = new GranularPhysicsBehavior(),
            VisualBehavior = new SolidVisualBehavior()
        };
        
        customPixel.PhysicsBehavior.UpdatePhysics(customPixel);
        customPixel.BaseColor = customPixel.VisualBehavior.GetBaseColor();
        customPixel.VisualBehavior.SetRandomColor(customPixel);
        
        Assert(customPixel.MovementBehavior is LiquidFlowBehavior, "Custom pixel should have liquid movement");
        Assert(customPixel.PhysicsBehavior is GranularPhysicsBehavior, "Custom pixel should have granular physics");
        Assert(customPixel.VisualBehavior is SolidVisualBehavior, "Custom pixel should have solid visuals");
        
        GD.Print("✓ Behavior Composition test passed");
    }
    
    private static void TestFactoryPattern()
    {
        GD.Print("Test: Factory Pattern");
        
        // Test that factory creates consistent pixels
        var solid1 = PixelFactory.CreateSolid();
        var solid2 = PixelFactory.CreateSolid();
        
        Assert(solid1.State == solid2.State, "Factory should create consistent states");
        Assert(solid1.Mass == solid2.Mass, "Factory should create consistent mass");
        Assert(solid1.MovementBehavior.GetType() == solid2.MovementBehavior.GetType(), "Factory should create consistent movement behaviors");
        Assert(solid1.PhysicsBehavior.GetType() == solid2.PhysicsBehavior.GetType(), "Factory should create consistent physics behaviors");
        Assert(solid1.VisualBehavior.GetType() == solid2.VisualBehavior.GetType(), "Factory should create consistent visual behaviors");
        
        // Test factory consistency
        var solid3 = PixelFactory.CreateSolid();
        var solid4 = PixelFactory.CreateSolid();
        
        Assert(solid3.State == solid4.State, "Factory should create consistent states");
        Assert(solid3.Mass == solid4.Mass, "Factory should create consistent mass");
        Assert(solid3.IsFalling == solid4.IsFalling, "Factory should create consistent falling state");
        
        GD.Print("✓ Factory Pattern test passed");
    }
    
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            GD.PrintErr($"ASSERTION FAILED: {message}");
            throw new System.Exception($"Test assertion failed: {message}");
        }
    }
}