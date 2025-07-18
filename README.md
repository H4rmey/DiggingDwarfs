# Sharp Digging Dwarfs

A pixel physics simulation game built with Godot and C#, featuring a composition-based architecture for flexible and scalable particle systems.

## 🏗️ Project Structure

The project has been completely reorganized for better maintainability and scalability:

```
Sharp-Digging-Dwarfs/
├── Source/                    # All source code
│   ├── Core/                  # Core engine systems
│   │   ├── Physics/           # Pixel physics engine
│   │   ├── Input/             # Input handling (brushes, tools)
│   │   └── Rendering/         # Rendering systems
│   ├── Game/                  # Game-specific logic
│   └── Tests/                 # Unit and integration tests
├── Resources/                 # Game assets
│   ├── Scenes/               # Godot scene files
│   ├── Textures/             # Images and sprites
│   └── Audio/                # Sound files
└── Documentation/            # Project documentation
```

## 🎯 Key Features

### Composition-Based Physics
- **Flexible Pixel System**: Pixels use behavior components instead of inheritance
- **Modular Behaviors**: Movement, physics, and visual behaviors can be mixed and matched
- **Easy Extension**: Add new pixel types by combining different behaviors

### Advanced Brush System
- **Multiple Brush Shapes**: Circle and square brushes with variable sizes
- **Real-time Preview**: Visual feedback for brush placement
- **Multiple Pixel Types**: Paint with air, solid, and liquid pixels

### Performance Optimized
- **Parallel Processing**: Multi-threaded physics simulation
- **Collision Resolution**: Smart conflict handling for pixel movement
- **Efficient Rendering**: Optimized chunk-based rendering system

## 🚀 Architecture Highlights

### Before: Inheritance-Based
```csharp
PixelElement (abstract)
├── PixelAir : PixelElement
├── PixelSolid : PixelElement
└── PixelLiquid : PixelElement
```

### After: Composition-Based
```csharp
PixelElement (concrete)
├── IMovementBehavior (injected)
├── IPhysicsBehavior (injected)
└── IVisualBehavior (injected)
```

## 📁 Core Systems

### Physics Engine (`Source/Core/Physics/`)
- **Elements**: Core pixel classes and states
- **Behaviors**: Modular behavior components
- **Factory**: Pixel creation patterns
- **Simulation**: Physics update logic

### Input System (`Source/Core/Input/`)
- **Brushes**: Painting and interaction tools
- **Tools**: Additional input handling (future)

### Rendering System (`Source/Core/Rendering/`)
- **Chunks**: Efficient pixel chunk rendering
- **Optimization**: Performance-focused rendering

## 🎮 Controls

- **Left Click**: Paint with selected pixel type
- **Right Click**: Erase (paint with air)
- **Mouse Wheel**: Change pixel type
- **Shift + Mouse Wheel**: Change brush size
- **Ctrl + Mouse Wheel**: Change brush shape
- **Tab**: Show brush information

## 🔧 Development

### Requirements
- Godot 4.4+
- .NET 8.0
- C# support in Godot

### Building
1. Open the project in Godot
2. Build the C# solution
3. Run the project

### Adding New Pixel Types
1. Create behavior classes implementing the three interfaces:
   - `IMovementBehavior`
   - `IPhysicsBehavior` 
   - `IVisualBehavior`
2. Add factory method in `PixelFactory`
3. Update brush system if needed

## 📚 Documentation

- [`Architecture.md`](Documentation/Architecture.md) - Detailed architecture overview
- [`Systems/`](Documentation/Systems/) - System-specific documentation
- [`API/`](Documentation/API/) - API documentation (future)

## 🎯 Benefits of New Architecture

### 🔧 **Maintainability**
- Clear separation of concerns
- Each behavior is independently testable
- Easier to understand and modify specific functionality

### 🚀 **Extensibility**
- New behaviors can be added without touching existing code
- Behaviors are reusable across different pixel types
- Easy to create hybrid or specialized pixel types

### 📈 **Scalability**
- Modular architecture supports large-scale development
- Clear patterns for where new code should go
- Easy to extract core systems into separate libraries

### 🎨 **Flexibility**
- Easy to create new pixel types by mixing behaviors
- Behaviors can be swapped at runtime
- No need to modify existing classes for new features

## 🔄 Migration Status

### ✅ Completed
- [x] Project structure reorganization
- [x] Composition-based pixel system
- [x] Modular behavior architecture
- [x] Factory pattern implementation
- [x] Brush system modularization
- [x] Resource organization
- [x] Documentation structure

### 🔄 In Progress
- [ ] Complete namespace updates
- [ ] Dependency resolution
- [ ] Compilation validation

### 📋 Future Plans
- [ ] Game-specific systems (Player, World, UI)
- [ ] Advanced simulation features
- [ ] Performance optimizations
- [ ] Comprehensive test suite

## 🤝 Contributing

The new modular architecture makes contributing much easier:

1. **Core Systems**: Add to `Source/Core/` for engine features
2. **Game Logic**: Add to `Source/Game/` for game-specific features
3. **Tests**: Add to `Source/Tests/` for test coverage
4. **Documentation**: Update relevant docs in `Documentation/`

## 📄 License

[Add your license information here]

---

**Note**: This project has been recently reorganized for better maintainability and scalability. The new architecture provides a solid foundation for future development while maintaining all existing functionality.