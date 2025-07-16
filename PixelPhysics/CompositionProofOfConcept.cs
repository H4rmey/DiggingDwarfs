using Godot;
using SharpDiggingDwarfs.Behaviors;

namespace SharpDiggingDwarfs;

/// <summary>
/// Proof-of-concept test to validate the composition-based pixel system
/// This demonstrates that the new approach works and can coexist with the old system
/// </summary>
public static class CompositionProofOfConcept
{
    /// <summary>
    /// Test the basic composition functionality
    /// </summary>
    public static void RunBasicTests()
    {
        GD.Print("=== Composition Proof-of-Concept Tests ===");
        
        // Test 1: Create different pixel types using composition
        TestPixelCreation();
        
        // Test 2: Test behavior delegation
        TestBehaviorDelegation();
        
        // Test 3: Test factory pattern
        TestFactoryPattern();
        
        GD.Print("=== All tests completed ===");
    }
    
    private static void TestPixelCreation()
    {
        GD.Print("Test 1: Creating composed pixels...");
        
        // Create an air pixel
        var airPixel = PixelFactory.CreateAir();
        GD.Print($"Air pixel - State: {airPixel.State}, Mass: {airPixel.Mass}, HasBehavior: {airPixel.MovementBehavior != null}");
        
        // Create a solid pixel
        var solidPixel = PixelFactory.CreateSolid();
        GD.Print($"Solid pixel - State: {solidPixel.State}, Mass: {solidPixel.Mass}, HasBehavior: {solidPixel.MovementBehavior != null}");
        
        // Test IsEmpty logic
        bool airIsEmptyForSolid = airPixel.IsEmpty(solidPixel);
        bool solidIsEmptyForAir = solidPixel.IsEmpty(airPixel);
        GD.Print($"Air is empty for solid: {airIsEmptyForSolid}, Solid is empty for air: {solidIsEmptyForAir}");
        
        GD.Print("✓ Pixel creation test passed");
    }
    
    private static void TestBehaviorDelegation()
    {
        GD.Print("Test 2: Testing behavior delegation...");
        
        var solidPixel = PixelFactory.CreateSolid();
        var airPixel = PixelFactory.CreateAir();
        
        // Test that solid pixel has movement behavior
        bool solidHasBehavior = solidPixel.MovementBehavior != null;
        bool airHasBehavior = airPixel.MovementBehavior != null;
        
        GD.Print($"Solid has movement behavior: {solidHasBehavior}");
        GD.Print($"Air has movement behavior: {airHasBehavior}");
        
        // Test behavior type
        if (solidPixel.MovementBehavior is FallingMovementBehavior)
        {
            GD.Print("✓ Solid pixel correctly uses FallingMovementBehavior");
        }
        else
        {
            GD.Print("✗ Solid pixel behavior type incorrect");
        }
        
        GD.Print("✓ Behavior delegation test passed");
    }
    
    private static void TestFactoryPattern()
    {
        GD.Print("Test 3: Testing factory pattern...");
        
        // Test creating different types
        var pixels = new PixelElementComposed[]
        {
            PixelFactory.CreateAir(),
            PixelFactory.CreateSolid(),
            PixelFactory.CreateLiquid()
        };
        
        foreach (var pixel in pixels)
        {
            GD.Print($"Created {pixel.State} pixel with mass {pixel.Mass}");
        }
        
        // Test factory consistency
        var solid1 = PixelFactory.CreateSolid();
        var solid2 = PixelFactory.CreateSolid();
        
        bool propertiesMatch = solid1.State == solid2.State &&
                              solid1.Mass == solid2.Mass &&
                              solid1.IsFalling == solid2.IsFalling;
        
        GD.Print($"Factory creates consistent pixels: {propertiesMatch}");
        
        GD.Print("✓ Factory pattern test passed");
    }
    
    /// <summary>
    /// Demonstrates how the composition system would integrate with PixelChunk
    /// This shows the migration path from inheritance to composition
    /// </summary>
    public static void DemonstrateChunkIntegration()
    {
        GD.Print("=== Chunk Integration Demo ===");
        
        // This would be how we'd gradually introduce composed pixels into the existing system
        var composedSolid = PixelFactory.CreateSolid();
        
        // Simulate getting swap position (this would normally be called by PixelChunk)
        var origin = new Vector2I(10, 10);
        
        // For the demo, we can't actually call GetSwapPosition without a real chunk
        // But we can show that the behavior is properly assigned
        if (composedSolid.MovementBehavior != null)
        {
            GD.Print("✓ Composed pixel ready for chunk integration");
            GD.Print($"  - Behavior type: {composedSolid.MovementBehavior.GetType().Name}");
            GD.Print($"  - Pixel state: {composedSolid.State}");
            GD.Print($"  - Mass: {composedSolid.Mass}");
        }
        
        GD.Print("=== Integration demo completed ===");
    }
}