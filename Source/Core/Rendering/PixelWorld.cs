using Godot;
using SharpDiggingDwarfs.Core.Input.Brushes;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Core.Rendering.Chunks;
using SharpDiggingDwarfs.Source.Core.Input;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class PixelWorld : Node2D
{
    private PixelChunk[,] _chunks;
    private HashSet<PixelChunk> _activeChunks;
    private Vector2I _chunkCount;
    public Vector2I chunkSize { get; private set; }

    private Vector2I _worldSize;      // the amount of pixel-element the world is
    private Vector2  _pixelSize;     
    private Vector2  _chunkScale;
    private Vector2  _windowSize;

    private Cam _cam;

    private BrushNode _brushNode;
    private List<(Vector2I, Vector2I)> _swaps;

    private const bool DEBUG_ENABLE_BORDERS = true;
    private const bool DEBUG_ENABLE_NEXT_PIXEL = false;
   
    public override void _Ready()
    {
        base._Ready();
        _chunkCount = new Vector2I(9, 9);
        chunkSize = new Vector2I(32, 18);

        //Position = new Vector2(0, -10);
        _windowSize   = GetViewport().GetVisibleRect().Size;
        _worldSize    = new Vector2I(chunkSize.X * _chunkCount.X, chunkSize.Y * _chunkCount.Y);
        _pixelSize    = new Vector2(_windowSize.X / _worldSize.X, _windowSize.Y / _worldSize.Y);
        //ChunkScale   = new Vector2(PixelSize.X / ChunkCount.X, PixelSize.Y / ChunkCount.Y);
        
        // set the camera
        PackedScene cameraScene = GD.Load<PackedScene>("res://Resources/Scenes/Cam.tscn");
        _cam = cameraScene.Instantiate<Cam>();
        _cam.world = this;
        _cam.ZoomChanged += ZoomChangedEventHandler;
        _cam.OffsetChanged += OffsetChangedEventHandler;
        _cam.Offset = new Vector2(_worldSize.X/2, _worldSize.Y/2);
        _cam.Zoom = _pixelSize;
        
        AddChild(_cam);
        
        _chunks = new PixelChunk[_chunkCount.X, _chunkCount.Y];
        _activeChunks = new HashSet<PixelChunk>();
        
        _swaps = new List<(Vector2I, Vector2I)>();
        
        for (int x = 0; x < _chunkCount.X; x++)
        {
            for (int y = 0; y < _chunkCount.Y; y++)
            {
                // create the chunk
                var chunkScene = GD.Load<PackedScene>("res://Resources/Scenes/PixelChunk.tscn");
                PixelChunk chunk = (PixelChunk)chunkScene.Instantiate();
                chunk.init(chunkSize, this, new Vector2I(x,y));
                
                SetChunkInactive(chunk);
                // place the chunk in the world
                AddChild(chunk);
                //if (DEBUG_ENABLE_BORDERS) chunk.DEBUG_DrawBorder(new Color(1,0,0,0.25f));
                _chunks[x, y] = chunk;
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
        foreach (PixelChunk chunk in _activeChunks.ToList())
        {
            if (chunk == null) continue;
            //List<(Vector2I, Vector2I)> swap = chunk.GetSwapPositionsAll();
            List<(Vector2I, Vector2I)> swap = chunk.GetSwapPositionsDirtyRect();
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
        foreach (var swap in swaps.OrderBy(_ => rng.Next()))
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
        PixelElement nextPixel    = GetPixelElementAt(next);
        
        SetPixelElementAt(next, currentPixel);
        SetPixelElementAt(current, nextPixel);
    }
    # endregion

    # region BRUSH
    private void InitBrush()
    {
        var brushScene = GD.Load<PackedScene>("res://Resources/Scenes/BrushNode.tscn");
        _brushNode = brushScene.Instantiate<BrushNode>();
        _brushNode.ParentWorld = this;
        _brushNode.init(_worldSize, _pixelSize, _windowSize);
        AddChild(_brushNode);

        _brushNode.PaintRequested += PaintRequestedEventHandler;
        _brushNode.EraseRequested += EraseRequestedEventHandler;
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
        PixelChunk chunk = GetChunkFromPixelPos(pos);
        _activeChunks.Add(chunk);
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
        
        PixelChunk chunk = GetChunkFromPixelPos(pos);
        _activeChunks.Add(chunk);
        
        // Generate all positions within the circle
        for (int x = size; x >= -size; x-=1)
        {
            for (int y = size; y >= -size; y-=1)
            {
                // Check if the position is within the circle using distance formula
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= size)
                {
                    Vector2I newPos = new Vector2I(pos.X + x, pos.Y +  y);
                    PixelElement pixel = _brushNode.pixels[pixelTypeIndex].Clone();
                    pixel.SetRandomColor();
                    SetPixelElementAt(newPos, pixel);
                }
            }
        }
        
        UpdateActiveChunks();
    }
    # endregion
    
    # region CHUNK
    // returns the chunk at a given world position
    // this functions expects a coordinate in the world not in the viewport
    public PixelChunk GetChunkFromPixelPos(Vector2I pos)
    {
        //int chunkWidth = WorldSize.X / ChunkCount.X;  
        //int chunkHeight = WorldSize.Y / ChunkCount.Y;

        int x = pos.X / chunkSize.X;
        int y = pos.Y / chunkSize.Y;
        //GD.Print(new Vector2I(x,y));

        if (IsInBoundChunk(new Vector2I(x, y)))
        {
            return _chunks[x, y];
        }
        else
        {
            return null;
        }
    }

    private void UpdateActiveChunks()
    {
        foreach (PixelChunk chunk in _activeChunks)
        {
            if (chunk == null) continue;
            chunk.UpdateLayers();
            //chunk.UpdateCollisions();
        }
    }
    # endregion 
    
    # region PIXEL
    // this functions expects a coordinate in the world not in the viewport
    public void SetPixelElementAt(Vector2I worldPos, PixelElement pixel)
    {
        if ( !IsInBoundPixel(worldPos)) { return; }
        
        PixelChunk chunk = GetChunkFromPixelPos(worldPos);
        if (chunk == null) return;
        SetChunkActive(chunk);
        chunk.UpdatePixel(new Vector2I( worldPos.X % chunkSize.X, worldPos.Y % chunkSize.Y), pixel);
        

        //int maxX = _chunks.GetLength(0);
        //int maxY = _chunks.GetLength(1);

        
        int x = chunk.worldPosition.X;
        int y = chunk.worldPosition.Y;
        // Check above
        if (y - 1 >= 0 && _chunks[x, y - 1] != null)
        {
            if (worldPos.Y % chunkSize.Y == 0 && y  > 0 && _chunks[x, y - 1] != null)
            {
                _activeChunks.Add(_chunks[x, y - 1]);
            }
        }
    }

    // this functions expects a coordinate in the world not in the viewport
    public PixelElement GetPixelElementAt(Vector2I pos)
    {
        PixelChunk chunk = GetChunkFromPixelPos(pos);
        if (chunk == null) return null;

        // get the local chunk coordinate
        int x = pos.X % chunkSize.X;
        int y = pos.Y % chunkSize.Y;
        
        return chunk.IsInBound(new Vector2I(x,y)) ? chunk.pixels[x, y] : null;
    }
    
    # endregion

    public void InitWorld()
    {
        for (int x = 0; x < _worldSize.X; x++)
        {
            for (int y = 0; y < _worldSize.Y; y++)
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
        ProcessSwaps(_swaps);

        _swaps.Clear();

        UpdateActiveChunks();
        _swaps = GetSwapsFromChunks();
    }
    
    
    
    # region MISC
    
    public Vector2I CamToWorld(Vector2 screenPos) { return new Vector2I((int)screenPos.X, (int)screenPos.Y); }
    public Vector2I ViewPortToWorld(Vector2I pos) { return new Vector2I((int)(pos.X/_windowSize.X*_worldSize.X),(int)(pos.Y/_windowSize.Y*_worldSize.Y)); }
    // this functions expects a coordinate in the world not in the viewport
    public Vector2I WorldToChunk(Vector2I pos) { return new Vector2I( pos.X % chunkSize.X, pos.Y % chunkSize.Y); }
    // checks if a pixel is inbound in the world
    // input is expect to be a coordinate in the world not the viewport
    public bool IsInBoundPixel(Vector2I pos) { return pos.X >= 0 && pos.X < _worldSize.X && pos.Y >= 0 && pos.Y < _worldSize.Y; }
    public bool IsInBoundChunk(Vector2I pos) { return pos.X >= 0 && pos.X < _chunkCount.X && pos.Y >= 0 && pos.Y < _chunkCount.Y; }
    public void SetChunkActive(PixelChunk chunk)
    {
        chunk?.SetIsActive(true);
        _activeChunks.Add(chunk);
    }
    public void SetChunkInactive(PixelChunk chunk)
    {
        chunk.SetIsActive(false);
        _activeChunks.Remove(chunk);
    }
    public PixelChunk GetChunkAt(Vector2I pos)
    {
        if (!IsInBoundChunk(pos))
            return null;
            
        return _chunks[pos.X, pos.Y];        
    }

    # endregion
}
