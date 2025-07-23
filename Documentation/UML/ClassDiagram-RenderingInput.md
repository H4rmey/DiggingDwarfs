# Rendering and Input Systems Class Diagram

```mermaid
classDiagram
    %% Rendering System
    class PixelChunk {
        +Vector2I Size
        -Image image
        -Vector2I mousePos
        -Vector2 viewPortSize
        -Sprite2D sprite
        -StaticBody2D staticBody
        +PixelElement[,] pixels
        -ConcurrentBag~(Vector2I, Vector2I)~ Swaps
        -BrushNode brushNode
        +_Ready()
        -SetupBrushNode()
        -OnBrushPaintRequested(Vector2I, int)
        -OnBrushEraseRequested(Vector2I)
        -OnBrushChanged(string, int, string)
        +_Input(InputEvent)
        +_PhysicsProcess(double)
        -RefreshFrame()
        -ProcessSwaps()
        -InitPixels()
        -InitImage()
        +SwapPixels((Vector2I, Vector2I))
        +SetPixel(int, int, PixelElement)
        +IsInBounds(int, int) bool
        +TestCompositionIntegration()
    }

    %% Brush System
    class IBrush {
        <<interface>>
        +GetAffectedPositions(Vector2I, int) List~Vector2I~
        +GetPreviewPositions(Vector2I, int) List~Vector2I~
        +Name string
    }

    class CircleBrush {
        +GetAffectedPositions(Vector2I, int) List~Vector2I~
        +GetPreviewPositions(Vector2I, int) List~Vector2I~
        +Name string
    }

    class SquareBrush {
        +GetAffectedPositions(Vector2I, int) List~Vector2I~
        +GetPreviewPositions(Vector2I, int) List~Vector2I~
        +Name string
    }

    class BrushManager {
        +const int MIN_BRUSH_SIZE
        +const int MAX_BRUSH_SIZE
        -List~IBrush~ availableBrushes
        -int currentBrushIndex
        -int currentSize
        -int currentPixelTypeIndex
        -PixelElement[] availablePixelTypes
        +IBrush CurrentBrush
        +int CurrentSize
        +PixelElement CurrentPixelType
        +string CurrentBrushName
        +string CurrentPixelTypeName
        +NextBrush()
        +PreviousBrush()
        +IncreaseBrushSize()
        +DecreaseBrushSize()
        +SetBrushSize(int)
        +NextPixelType()
        +PreviousPixelType()
        +GetPaintPositions(Vector2I) List~Vector2I~
        +GetPreviewPositions(Vector2I) List~Vector2I~
        +CreateCurrentPixel() PixelElement
        -GetPixelTypeName(PixelElement) string
        +GetDebugInfo() string
    }

    class BrushNode {
        +PaintRequested event
        +EraseRequested event
        +BrushChanged event
        -BrushManager brushManager
        -Vector2I chunkSize
        -Vector2I currentPosition
        +_Ready()
        +_Process(double)
        +_Input(InputEvent)
        +SetChunkSize(Vector2I)
        +GetPixelByIndex(int) PixelElement
        +GetBrushInfo() string
        -HandleMouseMovement(InputEventMouseMotion)
        -HandleMouseButton(InputEventMouseButton)
        -HandleKeypress(InputEvent)
        -UpdateCursorPosition(Vector2)
        -TryPaint()
        -TryErase()
    }

    %% Connections to Physics System
    class PixelElement {
        +PixelType Type
        +Color BaseColor
        +Color Color
        +PhysicsStatistics Statistics
        +PhysicsEnforcers Enforcers
        +IPixelBehaviour Behaviour
        +IVisualBehavior VisualBehavior
    }

    class PixelFactory {
        <<static>>
        +CreateAir() PixelElement
        +CreateSolid() PixelElement
        +CreateLiquid() PixelElement
        +CreateStructure() PixelElement
    }

    %% Relationships
    CircleBrush ..|> IBrush : implements
    SquareBrush ..|> IBrush : implements
    
    BrushManager o--> IBrush : manages
    BrushManager --> PixelElement : uses
    BrushManager --> PixelFactory : uses
    
    BrushNode --> BrushManager : has
    
    PixelChunk o--> PixelElement : contains
    PixelChunk --> BrushNode : has
    PixelChunk --> PixelFactory : uses
    
    BrushNode ..> PixelChunk : interacts with
```

This diagram illustrates the rendering and input systems of the DiggingDwarfs project. The `PixelChunk` class is the central rendering component that manages the pixel grid and handles physics updates. The brush system consists of the `IBrush` interface with `CircleBrush` and `SquareBrush` implementations, managed by the `BrushManager`. The `BrushNode` serves as the integration point with the Godot engine, handling user input and delegating to the brush manager.