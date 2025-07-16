# Brush System

This folder contains the new node-based brush system for the Sharp Digging Dwarfs pixel physics project.

## Features

- **Node-based architecture**: BrushNode can be used by any game object
- **Two brush shapes**: Circle and Square
- **Resizable brushes**: Size range from 1 to 32 pixels
- **Multiple pixel types**: Air, Solid, and Liquid
- **Real-time preview**: Shows where pixels will be placed
- **Signal-based communication**: Uses Godot signals for loose coupling
- **Reusable for player characters**: Perfect for player tools and abilities

## Controls

### Mouse Controls
- **Left Click**: Paint with the selected pixel type
- **Right Click**: Erase (paint with Air)
- **Mouse Move**: Shows brush preview

### Keyboard Controls
- **Mouse Wheel**: Change pixel type (Air → Solid → Liquid)
- **Shift + Mouse Wheel**: Change brush size (1-32)
- **Ctrl + Mouse Wheel**: Change brush shape (Circle ↔ Square)
- **Tab**: Print current brush settings to console

## Architecture

### Core Components

1. **IBrush**: Interface defining brush behavior
2. **BaseBrush**: Abstract base class with common functionality
3. **CircleBrush**: Circular brush implementation
4. **SquareBrush**: Square brush implementation
5. **BrushManager**: Manages brush selection, size, and pixel types

### Key Components

1. **BrushNode**: Main Godot Node2D that handles input and emits signals
2. **BrushManager**: Core logic for brush behavior and pixel management
3. **BrushNode.tscn**: Godot scene file for easy instantiation

### Signals

- **PaintRequested(Vector2I position, int pixelTypeIndex)**: Emitted when painting
- **EraseRequested(Vector2I position)**: Emitted when erasing
- **BrushChanged(string brushName, int size, string pixelType)**: Emitted when brush settings change

## Usage Example

```csharp
// Load and instantiate the brush node
var brushScene = GD.Load<PackedScene>("res://Brush/BrushNode.tscn");
var brushNode = brushScene.Instantiate<BrushNode>();

// Configure the brush
brushNode.SetChunkSize(new Vector2I(128, 72));

// Connect to signals
brushNode.PaintRequested += OnPaintRequested;
brushNode.EraseRequested += OnEraseRequested;

// Add to scene
AddChild(brushNode);

// Signal handlers
private void OnPaintRequested(Vector2I position, int pixelTypeIndex)
{
    var pixel = brushNode.GetPixelByIndex(pixelTypeIndex);
    SetPixel(position.X, position.Y, pixel);
}

private void OnEraseRequested(Vector2I position)
{
    SetPixel(position.X, position.Y, PixelFactory.CreateAir());
}
```

### For Player Characters

```csharp
// In your player character script
[Export] public PackedScene BrushScene;
private BrushNode playerBrush;

public override void _Ready()
{
    playerBrush = BrushScene.Instantiate<BrushNode>();
    playerBrush.SetInputEnabled(false); // Handle input manually
    AddChild(playerBrush);
    
    // Connect signals for player actions
    playerBrush.PaintRequested += OnPlayerPaint;
}

// Manual brush control
public void UseTool()
{
    playerBrush.PaintAt(GetPlayerPosition());
}
```

## Extending the System

To add a new brush shape:

1. Create a new class inheriting from `BaseBrush`
2. Implement `GetAffectedPositions()` and `GetPreviewPositions()`
3. Add it to the `availableBrushes` list in `BrushManager`

To add a new pixel type:

1. Create the pixel using `PixelFactory`
2. Add it to the `availablePixelTypes` array in `BrushManager`
3. Update `GetPixelTypeName()` if needed