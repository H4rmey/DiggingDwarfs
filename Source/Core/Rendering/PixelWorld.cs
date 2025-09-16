using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using Godot;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Core.Input.Brushes;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using Color = Godot.Color;

public partial class PixelWorld : Node2D
{
    public PixelChunk[,] Chunks;
    public HashSet<PixelChunk> ActiveChunks;
    public Vector2I ChunkCount = new Vector2I(20, 20);
    public Vector2I ChunkSize = new Vector2I(64, 36);

    public Vector2I WorldSize;
    public Vector2  WorldScale;
    public Vector2  ChunkScale;
    public Vector2  ViewPortSize;

    private BrushNode brushNode;
    
    //TODO: start temp code
    private Image image;
    private Sprite2D sprite;
    //TODO: end temp code
    
    public override void _Ready()
    {
        base._Ready();

        //Position = new Vector2(0, -10);

        ViewPortSize = GetViewport().GetVisibleRect().Size;
        WorldSize    = new Vector2I(ChunkSize.X * ChunkCount.X, ChunkSize.Y * ChunkCount.Y);
        WorldScale   = new Vector2(ViewPortSize.X / WorldSize.X, ViewPortSize.Y / WorldSize.Y);
        ChunkScale   = new Vector2(WorldScale.X / ChunkCount.X, WorldScale.Y / ChunkCount.Y);

        Chunks = new PixelChunk[ChunkCount.X, ChunkCount.Y];
        ActiveChunks = new HashSet<PixelChunk>();
        
        
        //TODO: start temp code
        sprite = new Sprite2D();
        image  = new Image();
        image  = Image.CreateEmpty(WorldSize.X, WorldSize.Y, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        sprite.Scale = WorldScale;
        sprite.Position = new Vector2(ViewPortSize.X / 2, ViewPortSize.Y / 2);
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
                float pos_x = (ViewPortSize.X / ChunkCount.X * x) + (ViewPortSize.X / ChunkCount.X)/2;
                float pos_y = (ViewPortSize.Y / ChunkCount.Y * y) + (ViewPortSize.Y / ChunkCount.Y)/2; 
                chunk.Position = new Vector2(pos_x, pos_y);
                chunk.Scale = WorldScale;
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
        GD.Print($"GetSwaps Time: {stopwatch.ElapsedMilliseconds} ms");

        GD.Print($"FPS: {Engine.GetFramesPerSecond()}");
        GD.Print($"SWP: {swaps.Count}");

        stopwatch.Restart();
        ProcessSwaps(swaps);
        stopwatch.Stop();
        GD.Print($"ProcessSwaps Time: {stopwatch.ElapsedMilliseconds} ms");

        swaps.Clear();

        stopwatch.Restart();
        DEBUG_RenderActiveChunkBorders(new Color(0, 0, 1, 0.25f));
        stopwatch.Stop();
        GD.Print($"DEBUG_RenderActiveChunkBorders Time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        SetImagesForChunks();
        stopwatch.Stop();
        GD.Print($"SetImagesForChunks Time: {stopwatch.ElapsedMilliseconds} ms");
    }
 
    
    
    private List<(Vector2I, Vector2I)> GetSwaps()
    {
        List<(Vector2I, Vector2I)> swaps = new List<(Vector2I, Vector2I)>();
            
        // get net positions
        foreach (PixelChunk chunk in ActiveChunks)
        {
            List<(Vector2I, Vector2I)> t_swap = chunk.GetSwapPositions();

            if (t_swap.Count == 0)
            {
                chunk.IsActive = false;
                ActiveChunks.Remove(chunk);
                
                DEBUG_RenderChunkBorder(chunk, new Color(1,0,0,0.25f));
                continue;
            }
            
            swaps.AddRange(t_swap);
        }

        return swaps;
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

    private void PaintRequestedEventHandler(Vector2I pos, int pixelTypeIndex)
    {
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

    public Vector2I ViewPortToWorld(Vector2I pos)
    {
        return new Vector2I((int)(pos.X/ViewPortSize.X*WorldSize.X),(int)(pos.Y/ViewPortSize.Y*WorldSize.Y)); 
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
