# Pixel Update Flow Sequence Diagram

```mermaid
sequenceDiagram
    participant Godot as Godot Engine
    participant PixelChunk
    participant PixelElement
    participant IPixelBehaviour
    participant ProcessSwaps as PixelChunk.ProcessSwaps
    
    Note over Godot,ProcessSwaps: Physics Update Cycle
    
    Godot->>PixelChunk: _PhysicsProcess(delta)
    activate PixelChunk
    
    PixelChunk->>PixelChunk: RefreshFrame()
    
    Note over PixelChunk: For each pixel in grid (parallelized)
    
    loop For each pixel (x,y) in parallel
        PixelChunk->>PixelElement: GetSwapPosition(origin, chunk)
        activate PixelElement
        
        PixelElement->>IPixelBehaviour: GetSwapPosition(origin, chunk, pixel)
        activate IPixelBehaviour
        
        Note over IPixelBehaviour: Behavior-specific movement logic
        IPixelBehaviour-->>PixelElement: (current, next) positions
        deactivate IPixelBehaviour
        
        PixelElement-->>PixelChunk: (current, next) positions
        deactivate PixelElement
        
        alt current != next
            PixelChunk->>PixelChunk: Swaps.Add((current, next))
        end
    end
    
    PixelChunk->>ProcessSwaps: ProcessSwaps()
    activate ProcessSwaps
    
    Note over ProcessSwaps: Identify and resolve conflicts
    
    ProcessSwaps->>ProcessSwaps: Track target positions
    ProcessSwaps->>ProcessSwaps: Identify conflicting swaps
    
    loop For each non-conflicting swap
        ProcessSwaps->>PixelChunk: SwapPixels(swap)
    end
    
    loop While conflicts exist
        ProcessSwaps->>ProcessSwaps: Re-evaluate conflicting swaps
        ProcessSwaps->>PixelElement: GetSwapPosition(conflict.current, chunk)
        ProcessSwaps->>ProcessSwaps: Apply valid swaps
        ProcessSwaps->>ProcessSwaps: Randomly resolve deadlocks
    end
    
    ProcessSwaps->>PixelChunk: Update sprite texture
    deactivate ProcessSwaps
    
    deactivate PixelChunk
```

This sequence diagram illustrates the physics update flow in the DiggingDwarfs project:

1. The Godot engine calls `_PhysicsProcess` on the `PixelChunk` every physics frame
2. `PixelChunk` calls `RefreshFrame` to update all pixels
3. For each pixel in the grid (in parallel):
   - The pixel's `GetSwapPosition` method is called
   - This delegates to the pixel's behavior component via the `IPixelBehaviour` interface
   - The behavior determines where the pixel should move to
   - If movement is needed, the swap is added to a collection
4. After all potential moves are calculated, `ProcessSwaps` handles the actual movement:
   - It identifies which swaps would conflict (multiple pixels trying to move to the same position)
   - It applies non-conflicting swaps immediately
   - For conflicting swaps, it recalculates and tries to resolve them
   - It handles potential deadlocks by randomly resolving some conflicts
5. Finally, it updates the sprite texture to reflect the new pixel states

This demonstrates how the composition-based architecture enables different movement behaviors while maintaining a unified update mechanism.