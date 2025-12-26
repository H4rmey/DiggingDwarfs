using Color = Godot.Color;
using Godot;
using SharpDiggingDwarfs.Core.Input.Brushes;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Source.Core.Input;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System;

public partial class PixelWorld : Node2D
{
    public PixelChunk[,] Chunks;
    public HashSet<PixelChunk> ActiveChunks;
    public Vector2I ChunkCount;
    public Vector2I ChunkSize;

    public Vector2I WorldSize;      // the amount of pixel-element the world is
    public Vector2  PixelSize;     
    public Vector2  ChunkScale;
    public Vector2  WindowSize;

    public Cam Cam;

    private BrushNode brushNode;
    private List<(Vector2I, Vector2I)> Swaps;

    private const bool DEBUG_ENABLE_BORDERS = true;
    private const bool DEBUG_ENABLE_NEXT_PIXEL = true;
   
    public override void _Ready()
    {
        base._Ready();
        ChunkCount = new Vector2I(5, 5);
        ChunkSize = new Vector2I(16, 9);

        //Position = new Vector2(0, -10);
        WindowSize   = GetViewport().GetVisibleRect().Size;
        WorldSize    = new Vector2I(ChunkSize.X * ChunkCount.X, ChunkSize.Y * ChunkCount.Y);
        PixelSize    = new Vector2(WindowSize.X / WorldSize.X, WindowSize.Y / WorldSize.Y);
        //ChunkScale   = new Vector2(PixelSize.X / ChunkCount.X, PixelSize.Y / ChunkCount.Y);
        
        // set the camera
        PackedScene cameraScene = GD.Load<PackedScene>("res://Resources/Scenes/Cam.tscn");
        Cam = cameraScene.Instantiate<Cam>();
        Cam.world = this;
        Cam.ZoomChanged += ZoomChangedEventHandler;
        Cam.OffsetChanged += OffsetChangedEventHandler;
        Cam.Offset = new Vector2(WorldSize.X/2, WorldSize.Y/2);
        Cam.Zoom = PixelSize;
        
        AddChild(Cam);
        
        Chunks = new PixelChunk[ChunkCount.X, ChunkCount.Y];
        ActiveChunks = new HashSet<PixelChunk>();
        
        Swaps = new List<(Vector2I, Vector2I)>();
        
        for (int x = 0; x < ChunkCount.X; x++)
        {
            for (int y = 0; y < ChunkCount.Y; y++)
            {
                // create the chunk
                var chunkScene = GD.Load<PackedScene>("res://Resources/Scenes/PixelChunk.tscn");
                PixelChunk chunk = (PixelChunk)chunkScene.Instantiate();
                chunk.Size = ChunkSize;
                
                // place the chunk in the correct position
                float pos_x = (ChunkSize.X * x) + ChunkSize.X / 2;
                float pos_y = (ChunkSize.Y * y) + ChunkSize.Y / 2; 
                chunk.Position = new Vector2(pos_x, pos_y);
                //chunk.Scale = PixelSize;
                chunk.WorldPosition = new Vector2I(x, y);
                chunk.ParentWorld = this;
                SetChunkInactive(chunk);
                //DEBUG_RenderChunkBorder(chunk, new Color(1,0,0,0.25f));
                
                // place the chunk in the world
                AddChild(chunk);
                //if (DEBUG_ENABLE_BORDERS) chunk.DEBUG_DrawBorder(new Color(1,0,0,0.25f));
                Chunks[x, y] = chunk;
            }
        }

        InitBrush();
        InitWorld();
        UpdateActiveChunks();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Enter)
        {
            GD.Print("Rendering Next Frame!");
            RefreshFrame();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        RefreshFrame();
    }
    
    # region SWAPS
    private List<(Vector2I, Vector2I)> GetSwapsFromChunks()
    {
        List<(Vector2I, Vector2I)> swaps = new();

        List<PixelChunk> chunksToRemove = new List<PixelChunk>();
        // convert ActiveChunks to a list so it is a copy
        foreach (PixelChunk chunk in ActiveChunks.ToList())
        {
            if (chunk == null) continue;
            List<(Vector2I, Vector2I)> swap = chunk.GetSwapPositions();
            if (swap.Count == 0)
            {
                chunksToRemove.Add(chunk);
                //if (DEBUG_ENABLE_BORDERS) chunk.DEBUG_DrawBorder(new Color(1, 0, 0, 0.25f));
                continue;
            }

            swaps.AddRange(swap);
        }

        foreach (PixelChunk chunk in chunksToRemove)
        {
            SetChunkInactive(chunk);
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
        foreach (var swap in swaps.OrderBy(x => rng.Next()))
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

    public void SwapPixels(Vector2I current, Vector2I next)
    {
        PixelElement currentPixel = GetPixelElementAt(current);
        PixelElement nextPixel = GetPixelElementAt(next);
        
        SetPixelElementAt(next, currentPixel);
        SetPixelElementAt(current, nextPixel);
        
    }
    # endregion

    # region BRUSH
    private void InitBrush()
    {
        var brushScene = GD.Load<PackedScene>("res://Resources/Scenes/BrushNode.tscn");
        brushNode = brushScene.Instantiate<BrushNode>();
        brushNode.ParentWorld = this;
        AddChild(brushNode);

        brushNode.PaintRequested += PaintRequestedEventHandler;
        brushNode.EraseRequested += EraseRequestedEventHandler;
    }

    private void ZoomChangedEventHandler(Vector2 zoom)
    {
        //PixelSize = (WindowSize / (Vector2)WorldSize) * Cam.Zoom;
        //PixelSize    = new Vector2(WindowSize.X / WorldSize.X, WindowSize.Y / WorldSize.Y);
    }
    
    private void OffsetChangedEventHandler(Vector2 offset)
    {
        
    }

    private void EraseRequestedEventHandler(Vector2I pos, int size)
    {
        pos = CamToWorld(pos);
        PixelChunk chunk = GetChunkFrom(pos);
        ActiveChunks.Add(chunk);
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
                    
                    if (!IsInBoundPixel(p)) continue;
                    
                    SetPixelElementAt(p, PixelFactory.CreateAir());
                }
            }
        }

        UpdateActiveChunks();
    }

    private void PaintRequestedEventHandler(Vector2I pos, int pixelTypeIndex, int size)
    {
        pos = CamToWorld(pos);
        
        PixelChunk chunk = GetChunkFrom(pos);
        ActiveChunks.Add(chunk);
        
        // Generate all positions within the circle
        for (int x = size; x >= -size; x-=1)
        {
            for (int y = size; y >= -size; y-=1)
            {
                // Check if the position is within the circle using distance formula
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= size)
                {
                    Vector2I p = new Vector2I(pos.X + x, pos.Y +  y);
                    SetPixelElementAt(p, brushNode.pixels[pixelTypeIndex].Clone());
                }
            }
        }
        
        UpdateActiveChunks();
    }
    # endregion
    
    # region CHUNK
    // returns the chunk at a given world position
    // this functions expects a coordinate in the world not in the viewport
    public PixelChunk GetChunkFrom(Vector2I pos)
    {
        //int chunkWidth = WorldSize.X / ChunkCount.X;  
        //int chunkHeight = WorldSize.Y / ChunkCount.Y;

        int x = pos.X / ChunkSize.X;
        int y = pos.Y / ChunkSize.Y;
        //GD.Print(new Vector2I(x,y));

        if (IsInBoundChunk(new Vector2I(x, y)))
        {
            return Chunks[x, y];
        }
        else
        {
            return null;
        }
    }

    private void UpdateActiveChunks()
    {
        foreach (PixelChunk chunk in ActiveChunks)
        {
            if (chunk == null) continue;
            chunk.texture.Update(chunk.image);
        }
    }
    # endregion 
    
    # region PIXEL
    // this functions expects a coordinate in the world not in the viewport
    public void SetPixelElementAt(Vector2I pos, PixelElement pixel)
    {
        if ( !IsInBoundPixel(pos)) { return; }
        
        PixelChunk chunk = GetChunkFrom(pos);
        if (chunk == null) return;

        SetChunkActive(chunk);
        
        int x = chunk.WorldPosition.X;
        int y = chunk.WorldPosition.Y;

        int maxX = Chunks.GetLength(0);
        int maxY = Chunks.GetLength(1);

        chunk.SetPixel(new Vector2I( pos.X % ChunkSize.X, pos.Y % ChunkSize.Y), pixel);
        
        //pixel.ExecuteOnPixel(this, pos + new Vector2I(0,-1), (executePixel, position) =>
        //{
        //    pixel.Process(this,position);
        //    //pixel.SetRandomColor();
        //});
        
        
        // Check above
        if (y - 1 >= 0 && Chunks[x, y - 1] != null)
            if (pos.Y % ChunkSize.Y == 0 && y  > 0 && Chunks[x, y - 1] != null)
            {
                ActiveChunks.Add(Chunks[x, y - 1]);
            }

        // Check below
        //if (y % ChunkSize.Y == 0 && y + 1 < maxY && Chunks[x, y + 1] != null)
        //{
        //    ActiveChunks.Add(Chunks[x, y + 1]);
        //}
        
    }

    // this functions expects a coordinate in the world not in the viewport
    public PixelElement GetPixelElementAt(Vector2I pos)
    {
        PixelChunk chunk = GetChunkFrom(pos);
        if (chunk == null) return null;

        // get the local chunk coordinate
        int x = pos.X % ChunkSize.X;
        int y = pos.Y % ChunkSize.Y;

        if (chunk.IsInBound(new Vector2I(x, y)))
        {
            return chunk.pixels[x, y];  
        }
        else
        {
            return null;
        }
    }
    
    # endregion

    public void InitWorld()
    {
        for (int x = 0; x < WorldSize.X; x++)
        {
            for (int y = 0; y < WorldSize.Y; y++)
            {
                SetPixelElementAt(new Vector2I(x,y), PixelFactory.CreateAir());
                //SetPixelElementAt(new Vector2I(x,y), PixelFactory.CreateSolid());
                //if (y < WorldSize.Y / 2)
                //{
                //    SetPixelElementAt(new Vector2I(x,y), PixelFactory.CreateSolid());
                //}
                //else
                //{
                //    SetPixelElementAt(new Vector2I(x,y), PixelFactory.CreateAir());
                //}
            }
        }
    }
    
    private void RefreshFrame()
    {
        Stopwatch stopwatch = new Stopwatch();

        ProcessSwaps(Swaps);

        Swaps.Clear();

        //DEBUG_RenderActiveChunkBorders(new Color(0, 0, 1, 0.25f));

        UpdateActiveChunks();
        Swaps = GetSwapsFromChunks();
    }
    
    # region MISC
    
    public Vector2I CamToWorld(Vector2 screenPos) { return new Vector2I((int)screenPos.X, (int)screenPos.Y); }

    public Vector2I ViewPortToWorld(Vector2I pos) { return new Vector2I((int)(pos.X/WindowSize.X*WorldSize.X),(int)(pos.Y/WindowSize.Y*WorldSize.Y)); }

    // this functions expects a coordinate in the world not in the viewport
    public Vector2I WorldToChunk(Vector2I pos) { return new Vector2I( pos.X % ChunkSize.X, pos.Y % ChunkSize.Y); }

    // checks if a pixel is inbound in the world
    // input is expect to be a coordinate in the world not the viewport
    public bool IsInBoundPixel(Vector2I pos) { return pos.X >= 0 && pos.X < WorldSize.X && pos.Y >= 0 && pos.Y < WorldSize.Y; }
    
    public bool IsInBoundChunk(Vector2I pos) { return pos.X >= 0 && pos.X < ChunkCount.X && pos.Y >= 0 && pos.Y < ChunkCount.Y; }

    public void SetChunkActive(PixelChunk chunk)
    {
        chunk.SetIsActive(true);
        ActiveChunks.Add(chunk);
    }
    public void SetChunkInactive(PixelChunk chunk)
    {
        chunk.SetIsActive(false);
        ActiveChunks.Remove(chunk);
    }

    # endregion
}
