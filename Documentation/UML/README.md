# DiggingDwarfs UML Documentation

This directory contains UML diagrams that document the architecture and flow of the DiggingDwarfs project.

## Contents

1. **[ArchitectureOverview.md](ArchitectureOverview.md)** - A comprehensive overview of the entire system architecture, including component relationships, architectural patterns, and future considerations.

2. **[ClassDiagram-Physics.md](ClassDiagram-Physics.md)** - Class diagram showing the composition-based physics system, including PixelElement, behaviors, and the factory pattern implementation.

3. **[ClassDiagram-RenderingInput.md](ClassDiagram-RenderingInput.md)** - Class diagram illustrating the rendering system (PixelChunk) and input system (Brushes, BrushManager, BrushNode).

4. **[SequenceDiagram-PixelUpdate.md](SequenceDiagram-PixelUpdate.md)** - Sequence diagram showing the pixel physics update flow during each physics frame.

5. **[SequenceDiagram-UserInteraction.md](SequenceDiagram-UserInteraction.md)** - Sequence diagram demonstrating how user input leads to pixel modification and brush/tool selection.

6. **[ViewerSetup.md](ViewerSetup.md)** - Instructions for setting up a browser-based viewer for the UML documentation.

## Key Architectural Insights

- **Composition over Inheritance**: The project uses a composition-based approach rather than inheritance, allowing for more flexible pixel behaviors.

- **Factory Pattern**: PixelFactory creates different types of pixels by combining appropriate behaviors.

- **Event-Driven Communication**: Components communicate through events to maintain loose coupling.

- **Parallel Processing**: The physics simulation uses parallel processing to improve performance.

## Viewing the Diagrams

These diagrams use Mermaid syntax, which can be rendered by:
- GitHub (automatic rendering in markdown)
- VS Code with the Mermaid extension
- Online Mermaid editors like https://mermaid.live/

### Browser-based Viewing

For a more interactive experience, you can set up a browser-based viewer:

1. See **[ViewerSetup.md](ViewerSetup.md)** for detailed instructions on creating an HTML viewer
2. The viewer provides navigation between diagrams and proper rendering of all Mermaid diagrams
3. Multiple setup options are provided, from simple local servers to professional documentation generators

## Future Documentation

As the project evolves, these diagrams should be updated to reflect new systems and interactions. In particular, the following areas may need additional documentation as they are implemented:

- Game-specific systems (Player, World, UI)
- Advanced simulation features
- Performance optimizations
- Additional tool systems