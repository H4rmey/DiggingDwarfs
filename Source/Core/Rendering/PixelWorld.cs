using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Input.Brushes;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Source.Core.Input;
using Color = Godot.Color;

public partial class PixelWorld : Node2D
{
    public PixelChunk[,] Chunks;
    public HashSet<PixelChunk> ActiveChunks;
    public Vector2I ChunkCount = new Vector2I(10, 10);
    public Vector2I ChunkSize = new Vector2I(64, 36);

    public Vector2I WorldSize;      // the amount of pixel-element the world is
    public Vector2  PixelSize;     
    public Vector2  ChunkScale;
    public Vector2  WindowSize;

    public Cam Cam;

    private BrushNode brushNode;
    
    //TODO: start temp code
    private Image image;
    private Sprite2D sprite;
    //TODO: end temp code
    
    public override void _Ready()
    {
        base._Ready();

        //Position = new Vector2(0, -10);
        WindowSize   = GetViewport().GetVisibleRect().Size;
        
        // set the camera
        PackedScene cameraScene = GD.Load<PackedScene>("res://Resources/Scenes/Cam.tscn");
        Cam = cameraScene.Instantiate<Cam>();
        Cam.world = this;
        Cam.ZoomChanged += ZoomChangedEventHandler;
        Cam.OffsetChanged += OffsetChangedEventHandler;
        
        AddChild(Cam);
        
        WorldSize    = new Vector2I(ChunkSize.X * ChunkCount.X, ChunkSize.Y * ChunkCount.Y);
        PixelSize   = new Vector2(WindowSize.X / WorldSize.X, WindowSize.Y / WorldSize.Y);
        ChunkScale   = new Vector2(PixelSize.X / ChunkCount.X, PixelSize.Y / ChunkCount.Y);
        

        Chunks = new PixelChunk[ChunkCount.X, ChunkCount.Y];
        ActiveChunks = new HashSet<PixelChunk>();
        
        
        //TODO: start temp code
        sprite = new Sprite2D();
        image  = new Image();
        image  = Image.CreateEmpty(WorldSize.X, WorldSize.Y, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        sprite.Scale = PixelSize;
        sprite.Position = new Vector2(WindowSize.X / 2, WindowSize.Y / 2);
        sprite.Texture = ImageTexture.CreateFromImage(image);
        //TODO: end temp code
        

        for (int x = 0; x < ChunkCount.X; x++)
        {
            for (int y = 0; y < ChunkCount.Y; y++)
            {
                // create the chunk
                var chunkScene = GD.Load<PackedScene>("res://Resources/Scenes/PixelChunk.tscn");
                PixelChunk chunk = (PixelChunk)chunkScene.Instantiate();
                chunk.Size = ChunkSize;
                
                // place the chunk in the correct position
                float pos_x = (WindowSize.X / ChunkCount.X * x) + (WindowSize.X / ChunkCount.X)/2;
                float pos_y = (WindowSize.Y / ChunkCount.Y * y) + (WindowSize.Y / ChunkCount.Y)/2; 
                chunk.Position = new Vector2(pos_x, pos_y);
                chunk.Scale = PixelSize;
                chunk.WorldPosition = new Vector2I(x, y);
                chunk.ParentWorld = this;
                chunk.IsActive = false;
                DEBUG_RenderChunkBorder(chunk, new Color(1,0,0,0.25f));
                
                // place the chunk in the world
                AddChild(chunk);
                Chunks[x, y] = chunk;
            }
        }

        InitBrush();
        
        AddChild(sprite);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        RefreshFrame();
    }

    private void RefreshFrame()
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        List<(Vector2I, Vector2I)> swaps = GetSwaps();
        stopwatch.Stop();
        //GD.Print($"GetSwaps Time: {stopwatch.ElapsedMilliseconds} ms");

        //GD.Print($"FPS: {Engine.GetFramesPerSecond()}");
        //GD.Print($"SWP: {swaps.Count}");

        stopwatch.Restart();
        ProcessSwaps(swaps);
        stopwatch.Stop();
        //GD.Print($"ProcessSwaps Time: {stopwatch.ElapsedMilliseconds} ms");

        swaps.Clear();

        stopwatch.Restart();
        DEBUG_RenderActiveChunkBorders(new Color(0, 0, 1, 0.25f));
        stopwatch.Stop();
        //GD.Print($"DEBUG_RenderActiveChunkBorders Time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        SetImagesForChunks();
        stopwatch.Stop();
        //GD.Print($"SetImagesForChunks Time: {stopwatch.ElapsedMilliseconds} ms");
    }
 
    
    
    private List<(Vector2I, Vector2I)> GetSwaps()
    {
        ConcurrentBag<(Vector2I, Vector2I)> swaps = new();
        HashSet<Vector2I> seenTargets = new();
        object lockObj = new();

        Parallel.ForEach(ActiveChunks, chunk =>
        {
            var t_swap = chunk.GetSwapPositions();
            if (t_swap.Count == 0)
            {
                ActiveChunks.Remove(chunk);
                chunk.IsActive = false;
                DEBUG_RenderChunkBorder(chunk, new Color(1, 0, 0, 0.25f));
                return;
            }

            foreach (var swap in t_swap)
            {
                lock (lockObj)
                {
                    if (seenTargets.Add(swap.Item2))
                        swaps.Add(swap);
                }
            }
        });

        return swaps.ToList();
    }
    
    private void ProcessSwaps(List<(Vector2I, Vector2I)> swaps)
    {
        // Track which positions are being targeted
        var targetPositions = new HashSet<Vector2I>();
        var processedSwaps = new List<(Vector2I, Vector2I)>();
        var conflictSwaps = new List<(Vector2I, Vector2I)>();
        var rng = new Random();

        // First pass: identify conflicts
        foreach (var swap in swaps)
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
            SwapPixels(swap.Item1, swap.Item2);
        }
    }

    private void SetImagesForChunks()
    {
        foreach (PixelChunk chunk in ActiveChunks)
        {
            chunk.sprite.Texture = ImageTexture.CreateFromImage(chunk.image);
        }
    }

    private void SwapPixels(Vector2I current, Vector2I next)
    {
        PixelElement currentPixel = GetPixelElementAt(current);
        PixelElement nextPixel = GetPixelElementAt(next);
        
        SetPixelElementAt(next, currentPixel);
        SetPixelElementAt(current, nextPixel);
    }


    private void InitBrush()
    {
        var brushScene = GD.Load<PackedScene>("res://Resources/Scenes/BrushNode.tscn");
        brushNode = brushScene.Instantiate<BrushNode>();
        brushNode.ParentWorld = this;
        AddChild(brushNode);

        brushNode.PaintRequested += PaintRequestedEventHandler;
    }

    private void ZoomChangedEventHandler(Vector2 zoom)
    {
        PixelSize = PixelSize * zoom;
    }
    
    private void OffsetChangedEventHandler(Vector2 offset)
    {
        
    }

    private void PaintRequestedEventHandler(Vector2I pos, int pixelTypeIndex)
    {
        Vector2 c = CamToWorld(pos);
        int size = 6;
        pos = ViewPortToWorld(pos);
        // Generate all positions within the circle
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                // Check if the position is within the circle using distance formula
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= size)
                {
                    Vector2I p = new Vector2I(pos.X + x, pos.Y +  y);
                    SetPixelElementAt(p, PixelFactory.CreateSolid());
                }
            }
        }
    }
    
    public Vector2I CamToWorld(Vector2 screenPos)
    {
        // Convert screen position to world position considering camera offset and zoom
        Vector2 adjustedPos = (screenPos - Cam.Offset) / Cam.Zoom;

        // Scale to world pixel coordinates
        int worldX = (int)(adjustedPos.X / PixelSize.X);
        int worldY = (int)(adjustedPos.Y / PixelSize.Y);

        Vector2I result =new Vector2I(worldX + (int)Cam.Offset.X/(int)PixelSize.Y, worldY + (int)Cam.Offset.Y/(int)PixelSize.Y);
        return result;
    }

    public Vector2I ViewPortToWorld(Vector2I pos)
    {
        return new Vector2I((int)(pos.X/WindowSize.X*WorldSize.X),(int)(pos.Y/WindowSize.Y*WorldSize.Y)); 
    }

    // this functions expects a coordinate in the world not in the viewport
    public Vector2I WorldToChunk(Vector2I pos)
    {
        return new Vector2I( pos.X % ChunkSize.X, pos.Y % ChunkSize.Y);
    }

    // returns the chunk at a given world position
    // this functions expects a coordinate in the world not in the viewport
    public PixelChunk GetChunkFrom(Vector2I pos)
    {
        int chunkWidth = WorldSize.X / ChunkCount.X;  
        int chunkHeight = WorldSize.Y / ChunkCount.Y;

        int x = pos.X / chunkWidth;
        int y = pos.Y / chunkHeight;

        if (IsInBound(new Vector2I(x, y)))
        {
            return Chunks[x, y];
        }
        else
        {
            return Chunks[0, 0];
        }
    }

    // this functions expects a coordinate in the world not in the viewport
    public void SetPixelElementAt(Vector2I pos, PixelElement pixel)
    {
        PixelChunk chunk = GetChunkFrom(pos);
        chunk.IsActive = true;
        ActiveChunks.Add(chunk);
        
        chunk.SetPixel(new Vector2I( pos.X % ChunkSize.X, pos.Y % ChunkSize.Y), pixel);
    }

    // this functions expects a coordinate in the world not in the viewport
    public PixelElement GetPixelElementAt(Vector2I pos)
    {
        PixelChunk chunk = GetChunkFrom(pos);

        // get the local chunk coordinate
        int x = pos.X % ChunkSize.X;
        int y = pos.Y % ChunkSize.Y;
        
        return chunk.pixels[x, y];  
    }

    // checks if a pixel is inbound in the world
    // input is expect to be a coordinate in the world not the viewport
    public bool IsInBound(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < WorldSize.X && pos.Y >= 0 && pos.Y < WorldSize.Y;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Enter)
        {
            GD.Print("Enter was pressed!");
            //RefreshFrame();
        }
    }

    private void DEBUG_RenderActiveChunkBorders(Color color)
    {
        // Only color the border of active chunks
        foreach (PixelChunk chunk in ActiveChunks)
        {
            DEBUG_RenderChunkBorder(chunk, color);
        }
        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

    private void DEBUG_RenderChunkBorder(PixelChunk chunk, Color color)
    {
        for (int x = 0; x < ChunkSize.X; x++)
        {
            for (int y = 0; y < ChunkSize.Y; y++)
            {
                Vector2I pos = chunk.ToWorldPosition(new Vector2I(x, y));

                if (x == 0 || y == 0 || x == ChunkSize.X - 1 || y == ChunkSize.Y - 1)
                {
                    image.SetPixel(pos.X, pos.Y, color); // Blue for active border
                }
            }
        }
    }
}
