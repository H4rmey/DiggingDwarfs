using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SharpDiggingDwarfs.Core.Input.Brushes;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Core.Physics.Behaviors;
using System.Text.Json;

namespace SharpDiggingDwarfs.Core.Rendering.Chunks;

public partial class PixelWorld : Node2D
{
    [ExportGroup("parameters")]
    [Export]
    public Vector2I ChunkSize = new Vector2I(16,16);
    [Export]
    public Vector2I ChunkAmount = new Vector2I(16*2, 9*2);
    public Vector2I WorldSize;

    private Image borderImage;
    private Vector2I mousePos;
    private Vector2 viewPortSize;

    private Node2D chunkContainer;
    private Sprite2D borderSprite;
    private StaticBody2D staticBody;
    
    // Mouse drawing functionality
    private bool isDrawing = false;
    
    // Brush system
    private BrushManager brushManager;

    // pdg = PixelDataGrid
    public PixelChunk[,] Chunks;
    
    // Global swap management
    private ConcurrentBag<(Vector2I, Vector2I)> GlobalSwaps = new ConcurrentBag<(Vector2I, Vector2I)>();
    
    // Performance optimization - track frame timing
    private int frameCounter = 0;
    private const int PhysicsUpdateInterval = 2; // Update physics every 2 frames

    public override void _Ready()
    {
        WorldSize = new Vector2I(ChunkSize.X * ChunkAmount.X, ChunkSize.Y * ChunkAmount.Y);
        
        // Create container for chunk sprites
        chunkContainer = new Node2D();
        AddChild(chunkContainer);
        
        staticBody = new StaticBody2D();
        AddChild(staticBody);

        // Create border image layer
        borderImage = new Image();
        borderImage = Image.CreateEmpty(WorldSize.X, WorldSize.Y, false, Image.Format.Rgba8);
        borderImage.Fill(Colors.Transparent);
        
        // Create border sprite
        borderSprite = new Sprite2D();
        AddChild(borderSprite);

        // Initialize the chunks array
        Chunks = new PixelChunk[ChunkAmount.X, ChunkAmount.Y];

        for (int x = 0; x < ChunkAmount.X; x++)
        {
            for (int y = 0; y < ChunkAmount.Y; y++)
            {
                PixelChunk chunk = new PixelChunk(this, new Vector2I(x,y), ChunkSize);
                
                Chunks[x, y] = chunk;
                
                // Add chunk sprite to the container and position it correctly
                Sprite2D chunkSprite = chunk.GetSprite();
                Vector2I worldPosition = new Vector2I(x * ChunkSize.X, y * ChunkSize.Y);
                chunkSprite.Position = new Vector2(worldPosition.X, worldPosition.Y);
                chunkContainer.AddChild(chunkSprite);
            }
        }

        viewPortSize = GetViewport().GetVisibleRect().Size;
        float width = (int)viewPortSize.X / WorldSize.X;
        float height = (int)viewPortSize.Y / WorldSize.Y;
        Scale = new Vector2(width, height);
        Position = new Vector2(viewPortSize.X / 2, viewPortSize.Y / 2);
        
        // Position the chunk container to match the border sprite positioning
        chunkContainer.Position = new Vector2(-WorldSize.X / 2, -WorldSize.Y / 2);
        chunkContainer.Scale = Scale;

        // Initialize brush manager
        brushManager = new BrushManager();
        
        // Initialize all chunk pixels
        InitPixels();
        
        // Update all chunk visuals
        UpdateAllChunkVisuals();
        
        GD.Print($"Brush system initialized: {brushManager.GetDebugInfo()}");
    }

    public override void _PhysicsProcess(double delta)
    {
        //frameCounter++;
        
        //// Only update physics every few frames for performance
        //if (frameCounter % PhysicsUpdateInterval == 0)
        //{
            RefreshFrame();
        //}
        
        // Always update visuals
        UpdateAllChunkVisuals();
        DrawChunkBorders();
    }

    public override void _Input(InputEvent @event)
    {
        // Handle mouse input for drawing pixels
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    isDrawing = true;
                    DrawPixelAtMousePosition(mouseButton.Position);
                }
                else
                {
                    isDrawing = false;
                }
            }
            
            // Handle brush input with right click
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                // Right click to cycle through pixel types
                brushManager.NextPixelType();
                GD.Print($"Switched to pixel type: {brushManager.CurrentPixelTypeName}");
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && isDrawing)
        {
            DrawPixelAtMousePosition(mouseMotion.Position);
        }
        else if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            HandleKeyboardInput(keyEvent);
        }
    }

    private void DrawPixelAtMousePosition(Vector2 screenPosition)
    {
        // Get the world's global transform
        Transform2D worldTransform = GlobalTransform;
        
        // Convert screen position to world local coordinates
        Vector2 worldLocalPos = worldTransform.AffineInverse() * screenPosition;
        
        // The world coordinates range from -WorldSize/2 to +WorldSize/2
        // Convert to pixel array coordinates (0 to WorldSize-1)
        Vector2I pixelPosition = new Vector2I(
            (int)(worldLocalPos.X + WorldSize.X / 2.0f),
            (int)(worldLocalPos.Y + WorldSize.Y / 2.0f)
        );
        
        // Clamp to valid bounds
        pixelPosition.X = Mathf.Clamp(pixelPosition.X, 0, WorldSize.X - 1);
        pixelPosition.Y = Mathf.Clamp(pixelPosition.Y, 0, WorldSize.Y - 1);
        
        // Draw pixel at the calculated position using brush system
        PaintWithBrush(pixelPosition);
    }

    public void SetPixelAtWorldPosition(Vector2I worldPosition, PixelType pixelType)
    {
        // Check if position is within bounds
        if (!IsInBounds(worldPosition.X, worldPosition.Y))
            return;

        // Get the chunk containing this position
        PixelChunk chunk = GetChunkAtWorldCoordinate(worldPosition);
        if (chunk == null) return;

        // Convert world coordinate to chunk coordinate
        Vector2I chunkPosition = WorldToChunkCoordinate(worldPosition, chunk);

        // Create the appropriate pixel using the factory
        PixelElement newPixel = pixelType switch
        {
            PixelType.Empty => PixelFactory.CreateAir(),
            PixelType.Solid => PixelFactory.CreateSolid(),
            PixelType.Liquid => PixelFactory.CreateLiquid(),
            _ => PixelFactory.CreateAir()
        };

        // Set the pixel in the chunk
        chunk.pixels[chunkPosition.X, chunkPosition.Y] = newPixel;

        // Update the chunk's visual immediately
        var targetChunk = GetChunkAtWorldCoordinate(worldPosition);
        if (targetChunk != null)
        {
            Vector2I chunkPos = WorldToChunkCoordinate(worldPosition, targetChunk);
            targetChunk.SetPixel(chunkPos.X, chunkPos.Y, newPixel);
        }
        
        // Mark chunk and surrounding chunks as active so physics will process them
        ActivateChunkAndNeighbors(chunk);
    }

    /// <summary>
    /// Activates a chunk and its neighboring chunks to ensure physics continues processing
    /// </summary>
    private void ActivateChunkAndNeighbors(PixelChunk targetChunk)
    {
        // Activate the target chunk
        if (!targetChunk.IsActive)
        {
            targetChunk.IsActive = true;
            targetChunk.BorderStateChanged = true;
        }
        
        // Get chunk coordinates
        Vector2I chunkCoord = targetChunk.Location;
        
        // Activate neighboring chunks
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2I neighborCoord = new Vector2I(chunkCoord.X + dx, chunkCoord.Y + dy);
                
                // Check if neighbor is within bounds
                if (neighborCoord.X >= 0 && neighborCoord.X < ChunkAmount.X &&
                    neighborCoord.Y >= 0 && neighborCoord.Y < ChunkAmount.Y)
                {
                    var neighborChunk = Chunks[neighborCoord.X, neighborCoord.Y];
                    if (!neighborChunk.IsActive)
                    {
                        neighborChunk.IsActive = true;
                        neighborChunk.BorderStateChanged = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Manages the global physics frame by collecting swaps from all chunks and processing them
    /// </summary>
    private void RefreshFrame()
    {
        GlobalSwaps.Clear();

        // Collect swap positions from all active chunks using parallel processing
        var allSwaps = new ConcurrentBag<(Vector2I, Vector2I)>();
        
        Parallel.For(0, ChunkAmount.X * ChunkAmount.Y, index =>
        {
            int chunkX = index % ChunkAmount.X;
            int chunkY = index / ChunkAmount.X;

            PixelChunk chunk = Chunks[chunkX, chunkY];
            if (chunk == null || !chunk.IsActive) return;

            var chunkSwaps = chunk.CalculateSwapPositions();

            if (chunkSwaps.Count == 0)
            {
                if (chunk.IsActive)
                {
                    chunk.IsActive = false;
                    chunk.BorderStateChanged = true;
                }
                return;
            }
            
            // Convert chunk coordinates to world coordinates and add to collection
            foreach (var swap in chunkSwaps)
            {
                Vector2I worldCurrent = ChunkToWorldCoordinate(swap.Item1, chunk);
                Vector2I worldNext = ChunkToWorldCoordinate(swap.Item2, chunk);

                allSwaps.Add((worldCurrent, worldNext));
            }
        });

        // Add all swaps to the global collection
        foreach (var swap in allSwaps)
        {
            GlobalSwaps.Add(swap);
        }

        // Process all swaps globally
        ProcessGlobalSwaps();
        
        // Deactivate chunks that only contain air pixels for performance
        DeactivateEmptyChunks();
    }

    /// <summary>
    /// Processes all global swaps with conflict resolution
    /// </summary>
    private void ProcessGlobalSwaps()
    {
        // Track which positions are being targeted
        var targetPositions = new HashSet<Vector2I>();
        var processedSwaps = new List<(Vector2I, Vector2I)>();
        var conflictSwaps = new List<(Vector2I, Vector2I)>();
        var rng = new Random();

        // First pass: identify conflicts
        foreach (var swap in GlobalSwaps.OrderBy(x => rng.Next()))
        {
            if (targetPositions.Add(swap.Item2))  // Item2 is the next position
            {
                // No conflict, add to processed swaps
                processedSwaps.Add(swap);
            }
            else
            {
                // Conflict detected, add to conflict list
                conflictSwaps.Add(swap);
            }
        }

        // Apply non-conflicting swaps
        foreach (var swap in processedSwaps)
        {
            SwapPixelsGlobal(swap);
        }

        // Handle conflicts by randomly selecting some to process
        if (conflictSwaps.Count > 0)
        {
            var resolvedConflicts = conflictSwaps
                .OrderBy(x => rng.Next())
                .Take(conflictSwaps.Count / 2)
                .ToList();

            foreach (var swap in resolvedConflicts)
            {
                SwapPixelsGlobal(swap);
            }
        }
    }

    /// <summary>
    /// Swaps pixels globally using world coordinates
    /// </summary>
    /// <param name="swap">Swap pair in world coordinates</param>
    private void SwapPixelsGlobal((Vector2I current, Vector2I next) swap)
    {
        Vector2I currentWorld = swap.current;
        Vector2I nextWorld = swap.next;

        // Check bounds
        if (!IsInBounds(currentWorld.X, currentWorld.Y) || !IsInBounds(nextWorld.X, nextWorld.Y))
            return;

        // Get the chunks containing these positions
        PixelChunk currentChunk = GetChunkAtWorldCoordinate(currentWorld);
        PixelChunk nextChunk = GetChunkAtWorldCoordinate(nextWorld);

        if (currentChunk == null || nextChunk == null) return;

        // Activate chunks if they are inactive to ensure the swap can take place
        if (!currentChunk.IsActive)
        {
            currentChunk.IsActive = true;
            DrawChunkBorders();
        }
        if (!nextChunk.IsActive)
        {
            nextChunk.IsActive = true;
            DrawChunkBorders();
        }

        // Convert world coordinates to chunk coordinates
        Vector2I currentChunkPos = WorldToChunkCoordinate(currentWorld, currentChunk);
        Vector2I nextChunkPos = WorldToChunkCoordinate(nextWorld, nextChunk);

        // Get the pixels
        PixelElement currentPixel = currentChunk.pixels[currentChunkPos.X, currentChunkPos.Y];
        PixelElement nextPixel = nextChunk.pixels[nextChunkPos.X, nextChunkPos.Y];

        if (currentPixel == null || nextPixel == null) return;

        // Perform the swap
        PixelElement tempCurrent = currentPixel.Clone();
        PixelElement tempNext = nextPixel.Clone();

        currentChunk.pixels[currentChunkPos.X, currentChunkPos.Y] = tempNext;
        nextChunk.pixels[nextChunkPos.X, nextChunkPos.Y] = tempCurrent;

        // Update the chunk visuals for both chunks
        currentChunk.ForceUpdate = true;
        nextChunk.ForceUpdate = true;
    }


    /// <summary>
    /// Updates visual representation of all chunks
    /// </summary>
    private void UpdateAllChunkVisuals()
    {
        // Only update chunks that are dirty, active, or need force update
        for (int chunkX = 0; chunkX < ChunkAmount.X; chunkX++)
        {
            for (int chunkY = 0; chunkY < ChunkAmount.Y; chunkY++)
            {
                PixelChunk chunk = Chunks[chunkX, chunkY];
                if (chunk == null) continue;

                // Only update if chunk is dirty, needs force update, or is active
                if (chunk.IsDirty || chunk.ForceUpdate || chunk.IsActive)
                {
                    chunk.UpdateVisuals();
                }
            }
        }
    }
    

    private void InitPixels()
    {
        // Set the values of all chunk pixels to Air
        for (int chunkX = 0; chunkX < ChunkAmount.X; chunkX++)
        {
            for (int chunkY = 0; chunkY < ChunkAmount.Y; chunkY++)
            {
                PixelChunk chunk = Chunks[chunkX, chunkY];
                if (chunk == null) continue;

                // Each chunk already initializes its own pixels to air in its constructor
                // But we can explicitly call InitPixels if needed for re-initialization
                chunk.InitPixels();
            }
        }
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < WorldSize.X && y >= 0 && y < WorldSize.Y;
    }

    /// <summary>
    /// Converts a world coordinate to the corresponding chunk coordinate within the specified chunk
    /// </summary>
    /// <param name="worldCoordinate">The world coordinate to convert</param>
    /// <param name="chunk">The chunk to convert the coordinate relative to</param>
    /// <returns>The local coordinate within the chunk</returns>
    public Vector2I WorldToChunkCoordinate(Vector2I worldCoordinate, PixelChunk chunk)
    {
        Vector2I chunkWorldStart = new Vector2I(chunk.Location.X * ChunkSize.X, chunk.Location.Y * ChunkSize.Y);
        return new Vector2I(worldCoordinate.X - chunkWorldStart.X, worldCoordinate.Y - chunkWorldStart.Y);
    }

    /// <summary>
    /// Converts a chunk coordinate to the corresponding world coordinate
    /// </summary>
    /// <param name="chunkCoordinate">The local coordinate within the chunk</param>
    /// <param name="chunk">The chunk containing the coordinate</param>
    /// <returns>The world coordinate</returns>
    public Vector2I ChunkToWorldCoordinate(Vector2I chunkCoordinate, PixelChunk chunk)
    {
        Vector2I chunkWorldStart = new Vector2I(chunk.Location.X * ChunkSize.X, chunk.Location.Y * ChunkSize.Y);
        return new Vector2I(chunkWorldStart.X + chunkCoordinate.X, chunkWorldStart.Y + chunkCoordinate.Y);
    }

    /// <summary>
    /// Gets the chunk that contains the specified world coordinate
    /// </summary>
    /// <param name="worldCoordinate">The world coordinate</param>
    /// <returns>The chunk containing the coordinate, or null if out of bounds</returns>
    public PixelChunk GetChunkAtWorldCoordinate(Vector2I worldCoordinate)
    {
        Vector2I chunkIndex = new Vector2I(worldCoordinate.X / ChunkSize.X, worldCoordinate.Y / ChunkSize.Y);
        
        if (chunkIndex.X >= 0 && chunkIndex.X < ChunkAmount.X &&
            chunkIndex.Y >= 0 && chunkIndex.Y < ChunkAmount.Y)
        {
            return Chunks[chunkIndex.X, chunkIndex.Y];
        }
        
        return null;
    }

    /// <summary>
    /// Gets the pixel element at the specified world coordinate
    /// </summary>
    /// <param name="worldCoordinate">The world coordinate</param>
    /// <returns>The pixel element, or null if out of bounds</returns>
    public PixelElement GetPixelAtWorldCoordinate(Vector2I worldCoordinate)
    {
        PixelChunk chunk = GetChunkAtWorldCoordinate(worldCoordinate);
        if (chunk == null) return null;

        Vector2I chunkCoordinate = WorldToChunkCoordinate(worldCoordinate, chunk);
        if (!chunk.IsInBounds(chunkCoordinate.X, chunkCoordinate.Y)) return null;

        return chunk.pixels[chunkCoordinate.X, chunkCoordinate.Y];
    }

    /// <summary>
    /// Checks if a pixel can move to the specified world position
    /// </summary>
    /// <param name="worldCoordinate">The world coordinate to check</param>
    /// <param name="movingPixel">The pixel that wants to move</param>
    /// <returns>True if the position is empty or can be swapped</returns>
    public bool CanMoveToWorldPosition(Vector2I worldCoordinate, PixelElement movingPixel)
    {
        if (!IsInBounds(worldCoordinate.X, worldCoordinate.Y)) return false;
        if (movingPixel == null) return false;

        PixelElement targetPixel = GetPixelAtWorldCoordinate(worldCoordinate);
        if (targetPixel == null) return false;

        return targetPixel.IsEmpty(movingPixel);
    }

    /// <summary>
    /// Draws borders around all chunks on the border image layer with colors based on activation state
    /// </summary>
    private void DrawChunkBorders()
    {
        // Only redraw borders if they've changed
        bool bordersChanged = false;
        
        // Draw chunk borders with colors based on activation state
        for (int chunkX = 0; chunkX < ChunkAmount.X; chunkX++)
        {
            for (int chunkY = 0; chunkY < ChunkAmount.Y; chunkY++)
            {
                PixelChunk chunk = Chunks[chunkX, chunkY];
                if (chunk == null) continue;
                
                // Skip if chunk activation state hasn't changed
                if (!chunk.BorderStateChanged) continue;
                
                bordersChanged = true;
                chunk.BorderStateChanged = false;
                
                Color borderColor = chunk.IsActive ? new Color(0, 0, 1, 0.5f) : new Color(1, 0, 0, 0.5f);
                
                // Draw chunk borders
                int startX = chunkX * ChunkSize.X;
                int startY = chunkY * ChunkSize.Y;
                int endX = startX + ChunkSize.X - 1;
                int endY = startY + ChunkSize.Y - 1;
                
                // Draw top border
                for (int x = startX; x <= endX; x++)
                {
                    if (IsInBounds(x, startY))
                        borderImage.SetPixel(x, startY, borderColor);
                }
                
                // Draw bottom border
                for (int x = startX; x <= endX; x++)
                {
                    if (IsInBounds(x, endY))
                        borderImage.SetPixel(x, endY, borderColor);
                }
                
                // Draw left border
                for (int y = startY; y <= endY; y++)
                {
                    if (IsInBounds(startX, y))
                        borderImage.SetPixel(startX, y, borderColor);
                }
                
                // Draw right border
                for (int y = startY; y <= endY; y++)
                {
                    if (IsInBounds(endX, y))
                        borderImage.SetPixel(endX, y, borderColor);
                }
            }
        }

        // Only update texture if borders actually changed
        if (bordersChanged)
        {
            // Update the border sprite texture
            borderSprite.Texture = ImageTexture.CreateFromImage(borderImage);
            
            // Make sure border sprite matches the world transform exactly
            borderSprite.GlobalTransform = GlobalTransform;
        }
    }
    /// <summary>
    /// Deactivates chunks that only contain air pixels to improve performance
    /// </summary>
    private void DeactivateEmptyChunks()
    {
        bool anyChunkDeactivated = false;
        
        for (int chunkX = 0; chunkX < ChunkAmount.X; chunkX++)
        {
            for (int chunkY = 0; chunkY < ChunkAmount.Y; chunkY++)
            {
                PixelChunk chunk = Chunks[chunkX, chunkY];
                if (chunk == null || !chunk.IsActive) continue;
                
                if (IsChunkOnlyAir(chunk))
                {
                    chunk.IsActive = false;
                    chunk.BorderStateChanged = true;
                    anyChunkDeactivated = true;
                }
            }
        }
        
        // Only redraw borders if chunks were actually deactivated
        if (anyChunkDeactivated)
        {
            DrawChunkBorders();
        }
    }
    
    /// <summary>
    /// Checks if a chunk contains only air pixels
    /// </summary>
    /// <param name="chunk">The chunk to check</param>
    /// <returns>True if the chunk contains only air pixels</returns>
    private bool IsChunkOnlyAir(PixelChunk chunk)
    {
        for (int x = 0; x < ChunkSize.X; x++)
        {
            for (int y = 0; y < ChunkSize.Y; y++)
            {
                PixelElement pixel = chunk.pixels[x, y];
                if (pixel == null) continue;
                
                // If any pixel is not air, the chunk is not empty
                if (pixel.Type != PixelType.Empty)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Handles keyboard input for brush controls
    /// </summary>
    private void HandleKeyboardInput(InputEventKey keyEvent)
    {
        // Number keys 1-9 for pixel types
        if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
        {
            int typeIndex = (int)(keyEvent.Keycode - Key.Key1);
            if (typeIndex < brushManager.AvailablePixelTypesCount)
            {
                brushManager.SetPixelTypeIndex(typeIndex);
                GD.Print($"Selected pixel type: {brushManager.CurrentPixelTypeName}");
            }
        }
        
        // Ctrl+ and Ctrl- for brush size
        if (keyEvent.CtrlPressed)
        {
            if (keyEvent.Keycode == Key.Plus || keyEvent.Keycode == Key.Equal)
            {
                brushManager.IncreaseBrushSize();
                GD.Print($"Brush size increased to: {brushManager.CurrentSize}");
            }
            else if (keyEvent.Keycode == Key.Minus)
            {
                brushManager.DecreaseBrushSize();
                GD.Print($"Brush size decreased to: {brushManager.CurrentSize}");
            }
        }
        
        // C for circle mode, S for square mode
        if (keyEvent.Keycode == Key.C)
        {
            brushManager.SetBrushIndex(0); // Circle brush
            GD.Print($"Brush mode: Circle");
        }
        else if (keyEvent.Keycode == Key.S)
        {
            brushManager.SetBrushIndex(1); // Square brush
            GD.Print($"Brush mode: Square");
        }
    }
    
    /// <summary>
    /// Paints using the current brush settings at the specified position
    /// </summary>
    private void PaintWithBrush(Vector2I centerPosition)
    {
        // Get all positions affected by the brush
        var paintPositions = brushManager.GetPaintPositions(centerPosition);
        
        foreach (var position in paintPositions)
        {
            // Check if position is within bounds
            if (!IsInBounds(position.X, position.Y))
                continue;
                
            // Get the chunk containing this position
            PixelChunk chunk = GetChunkAtWorldCoordinate(position);
            if (chunk == null) continue;
            
            // Convert world coordinate to chunk coordinate
            Vector2I chunkPosition = WorldToChunkCoordinate(position, chunk);
            
            // Create the appropriate pixel using the brush manager
            PixelElement newPixel = brushManager.CreateCurrentPixel();
            
            // Set the pixel in the chunk
            chunk.pixels[chunkPosition.X, chunkPosition.Y] = newPixel;
            
            // Update the chunk immediately
            chunk.SetPixel(chunkPosition.X, chunkPosition.Y, newPixel);
            
            // Mark chunk and surrounding chunks as active so physics will process them
            ActivateChunkAndNeighbors(chunk);
        }
    }
}