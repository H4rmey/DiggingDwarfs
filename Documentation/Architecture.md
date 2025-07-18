# Sharp Digging Dwarfs - Project Architecture

## Overview

This document describes the reorganized architecture of the Sharp Digging Dwarfs pixel physics game project. The project has been restructured from a flat, inheritance-based system to a modular, composition-based architecture with clear separation of concerns.

## Project Structure

```
Sharp-Digging-Dwarfs/
├── Source/                           # All source code
│   ├── Core/                         # Core engine systems
│   │   ├── Physics/                  # Physics engine
│   │   │   ├── Elements/             # Pixel elements and states
│   │   │   ├── Behaviors/            # Behavior components
│   │   │   ├── Simulation/           # Simulation logic (future)
│   │   │   └── Factory/              # Creation patterns
│   │   │
│   │   ├── Input/                    # Input handling
│   │   │   ├── Brushes/              # Brush system
│   │   │   └── Tools/                # Tool system (future)
│   │   │
│   │   └── Rendering/                # Rendering systems
│   │       └── Chunks/               # Chunk rendering
│   │
│   ├── Game/                         # Game-specific logic (future)
│   │   ├── World/                    # World management
│   │   ├── Player/                   # Player systems
│   │   └── UI/                       # User interface
│   │
│   └── Tests/                        # Test code
│       ├── Unit/                     # Unit tests
│       └── Integration/              # Integration tests
│
├── Resources/                        # Game resources
│   ├── Scenes/                       # .tscn files
│   ├── Textures/                     # Images and sprites
│   └── Audio/                        # Sound files
│
└── Documentation/                    # Project documentation
    ├── Architecture.md               # This file
    ├── Systems/                      # System-specific docs
    └── API/                          # API documentation (future)
```

## Namespace Organization

The project uses a hierarchical namespace structure that mirrors the folder organization:

### Core Systems
- `SharpDiggingDwarfs.Core.Physics.Elements` - Pixel elements and states
- `SharpDiggingDwarfs.Core.Physics.Behaviors` - Behavior components
- `SharpDiggingDwarfs.Core.Physics.Factory` - Factory patterns
- `SharpDiggingDwarfs.Core.Input.Brushes` - Brush system
- `SharpDiggingDwarfs.Core.Rendering.Chunks` - Chunk rendering

### Game Systems (Future)
- `SharpDiggingDwarfs.Game.World` - World management
- `SharpDiggingDwarfs.Game.Player` - Player systems
- `SharpDiggingDwarfs.Game.UI` - User interface

### Testing
- `SharpDiggingDwarfs.Tests.Unit` - Unit tests
- `SharpDiggingDwarfs.Tests.Integration` - Integration tests

## Architecture Patterns

### 1. Composition over Inheritance

The pixel physics system has been refactored from inheritance to composition:

**Old System:**
```csharp
PixelElement (abstract)
├── PixelAir : PixelElement
├── PixelSolid : PixelElement
└── PixelLiquid : PixelElement
```

**New System:**
```csharp
PixelElement (concrete)
├── IMovementBehavior (injected)
├── IPhysicsBehavior (injected)
└── IVisualBehavior (injected)
```

### 2. Behavior Components

Three main behavior interfaces define pixel capabilities:

- **IMovementBehavior** - How pixels move in the simulation
- **IPhysicsBehavior** - Physics properties (mass, friction, etc.)
- **IVisualBehavior** - Visual appearance and color management

### 3. Factory Pattern

The `PixelFactory` creates different pixel types by combining appropriate behaviors:

```csharp
// Air pixel - static, no mass, subtle visual
var airPixel = PixelFactory.CreateAir();

// Solid pixel - falls, has mass, granular physics
var solidPixel = PixelFactory.CreateSolid();

// Liquid pixel - flows, has mass, fluid physics
var liquidPixel = PixelFactory.CreateLiquid();
```

## Key Components

### Core Physics Elements

#### PixelElement
- Main pixel class using composition
- Contains behavior references and core properties
- Delegates behavior to injected components

#### PixelState
- Enum defining pixel states (Empty, Solid, Liquid, Gas)
- Used for type identification and factory creation

### Behavior System

#### Movement Behaviors
- `StaticMovementBehavior` - For air/static pixels
- `FallingMovementBehavior` - For solid particles
- `LiquidFlowBehavior` - For liquid flow simulation

#### Physics Behaviors
- `EmptyPhysicsBehavior` - For air (no mass/friction)
- `GranularPhysicsBehavior` - For solid particles
- `FluidPhysicsBehavior` - For liquid physics

#### Visual Behaviors
- `AirVisualBehavior` - Subtle air visualization
- `SolidVisualBehavior` - Solid particle colors
- `LiquidVisualBehavior` - Liquid appearance

### Rendering System

#### PixelChunk
- Main simulation and rendering component
- Manages pixel grid and physics updates
- Integrates with brush system for user interaction
- Handles parallel processing and collision resolution

### Input System

#### Brush System
- `IBrush` interface for different brush shapes
- `BrushManager` for brush selection and configuration
- `BrushNode` for Godot integration and input handling
- Support for circle and square brushes with variable sizes

## Benefits of New Architecture

### 🎯 Flexibility
- Easy to create new pixel types by mixing behaviors
- Behaviors can be swapped at runtime
- No need to modify existing classes for new features

### 🔧 Maintainability
- Clear separation of concerns
- Each behavior is independently testable
- Easier to understand and modify specific functionality

### 🚀 Extensibility
- New behaviors can be added without touching existing code
- Behaviors are reusable across different pixel types
- Easy to create hybrid or specialized pixel types

### 📚 Scalability
- Modular architecture supports large-scale development
- Clear patterns for where new code should go
- Easy to extract core systems into separate libraries

## Migration Status

### ✅ Completed
- [x] New folder structure created
- [x] Files reorganized into logical modules
- [x] Namespace hierarchy established
- [x] Core behavior interfaces defined
- [x] Factory pattern implemented
- [x] Brush system modularized
- [x] Resources properly organized
- [x] Documentation structure created

### 🔄 In Progress
- [ ] Complete namespace updates for all behavior classes
- [ ] Update all import statements and dependencies
- [ ] Validate compilation and functionality

### 📋 Future Enhancements
- [ ] Game-specific systems (Player, World, UI)
- [ ] Advanced simulation features
- [ ] Performance optimizations
- [ ] Additional tool systems
- [ ] Comprehensive test suite

## Development Guidelines

### Adding New Pixel Types
1. Create behavior classes implementing the three interfaces
2. Add factory method in `PixelFactory`
3. Update brush system if needed
4. Add tests for new behaviors

### Adding New Behaviors
1. Implement appropriate behavior interface
2. Add to factory methods as needed
3. Ensure proper namespace and using statements
4. Write unit tests for the behavior

### File Organization Rules
- Core engine code goes in `Source/Core/`
- Game-specific code goes in `Source/Game/`
- Tests go in `Source/Tests/`
- Resources go in `Resources/`
- Documentation goes in `Documentation/`

## Conclusion

The reorganized architecture provides a solid foundation for the Sharp Digging Dwarfs project. The composition-based approach offers much greater flexibility than the previous inheritance system, while the modular structure makes the codebase more maintainable and scalable.

The clear separation between core engine systems and game-specific logic will make it easier to extend the project in the future, and the behavior-based pixel system opens up many possibilities for creating interesting and complex particle interactions.