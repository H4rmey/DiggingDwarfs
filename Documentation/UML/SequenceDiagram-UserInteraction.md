# User Interaction Flow Sequence Diagram

```mermaid
sequenceDiagram
    participant User
    participant Godot as Godot Engine
    participant BrushNode
    participant BrushManager
    participant IBrush
    participant PixelChunk
    participant PixelFactory
    
    Note over User,PixelFactory: Painting Interaction Flow
    
    User->>Godot: Mouse movement
    Godot->>BrushNode: InputEventMouseMotion
    activate BrushNode
    
    BrushNode->>BrushNode: HandleMouseMovement()
    BrushNode->>BrushNode: UpdateCursorPosition()
    deactivate BrushNode
    
    User->>Godot: Mouse click (left button)
    Godot->>BrushNode: InputEventMouseButton
    activate BrushNode
    
    BrushNode->>BrushNode: HandleMouseButton()
    BrushNode->>BrushNode: TryPaint()
    
    BrushNode->>BrushManager: GetPaintPositions(position)
    activate BrushManager
    
    BrushManager->>IBrush: GetAffectedPositions(position, size)
    activate IBrush
    
    IBrush-->>BrushManager: List of affected positions
    deactivate IBrush
    
    BrushManager-->>BrushNode: List of affected positions
    deactivate BrushManager
    
    BrushNode->>BrushManager: CurrentPixelType
    activate BrushManager
    BrushManager-->>BrushNode: Current pixel type index
    deactivate BrushManager
    
    loop For each affected position
        BrushNode->>PixelChunk: PaintRequested event (position, pixelTypeIndex)
        activate PixelChunk
        
        PixelChunk->>BrushNode: GetPixelByIndex(pixelTypeIndex)
        activate BrushNode
        
        BrushNode->>BrushManager: CreateCurrentPixel()
        activate BrushManager
        BrushManager-->>BrushNode: Pixel clone
        deactivate BrushManager
        
        BrushNode-->>PixelChunk: PixelElement
        deactivate BrushNode
        
        PixelChunk->>PixelChunk: SetPixel(x, y, pixel)
        
        Note over PixelChunk: Pixel.SetRandomColor()
        Note over PixelChunk: Update image
        
        PixelChunk->>PixelChunk: Check surrounding pixels
        deactivate PixelChunk
    end
    deactivate BrushNode
    
    Note over User,PixelFactory: Brush/Tool Selection Flow
    
    User->>Godot: Key press (change brush)
    Godot->>BrushNode: InputEvent
    activate BrushNode
    
    BrushNode->>BrushNode: HandleKeypress()
    
    alt Change brush type
        BrushNode->>BrushManager: NextBrush() or PreviousBrush()
        activate BrushManager
        BrushManager->>BrushManager: Update currentBrushIndex
        BrushManager-->>BrushNode: Done
        deactivate BrushManager
    else Change brush size
        BrushNode->>BrushManager: IncreaseBrushSize() or DecreaseBrushSize()
        activate BrushManager
        BrushManager->>BrushManager: Update currentSize
        BrushManager-->>BrushNode: Done
        deactivate BrushManager
    else Change pixel type
        BrushNode->>BrushManager: NextPixelType() or PreviousPixelType()
        activate BrushManager
        BrushManager->>BrushManager: Update currentPixelTypeIndex
        BrushManager-->>BrushNode: Done
        deactivate BrushManager
    end
    
    BrushNode->>PixelChunk: BrushChanged event
    deactivate BrushNode
```

This sequence diagram illustrates the user interaction flow in the DiggingDwarfs project:

1. **Painting Interaction**:
   - User moves the mouse and clicks to paint
   - Input events are captured by the Godot engine and passed to BrushNode
   - BrushNode uses BrushManager to determine affected positions
   - For each position, a PaintRequested event is triggered
   - PixelChunk creates and places the appropriate pixel

2. **Brush/Tool Selection**:
   - User presses keys to change brush properties
   - BrushNode handles these inputs and updates BrushManager
   - BrushManager updates its internal state (brush type, size, pixel type)
   - BrushNode emits a BrushChanged event to notify observers

This demonstrates how the input system is integrated with the rendering system, using events to communicate between components while maintaining separation of concerns.