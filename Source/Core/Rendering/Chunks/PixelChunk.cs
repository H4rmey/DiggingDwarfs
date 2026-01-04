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
using SharpDiggingDwarfs.Core.Rendering;
using SharpDiggingDwarfs.Core.Physics.Behaviors;
using System.Text.Json;
using SharpDiggingDwarfs.Source.Core.Rendering;
using SharpDiggingDwarfs.Source.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Rendering.Chunks;

public partial class PixelChunk : Node2D
{
    private Vector2I _size = new Vector2I(32, 18);
    public Vector2I worldPosition { get; private set; }
    public PixelWorld parentWorld { get; private set; }
    private PixelChunkLayerManager _layerManager;
    
    private Vector2I _mousePos;
    private Vector2 _viewPortSize;
    
    public PixelElement[,] pixels { get; private set; }
    private PixelCollision _collision;

    private List<(Vector2I, Vector2I)> _swaps = new();

    private const bool DEBUG_DRAW_BORDERS = false;
    private DebugImage debugBorders;
    
    private const bool DEBUG_DRAW_PIXELS = false;
    public DebugImage debugPixels;

    private bool IsActive = true;
    private PixelDirtyRect _pixelDirtyRect;
    
    public override void _Ready()
    {
        _layerManager   = new PixelChunkLayerManager();
        _collision      = new PixelCollision(); 
        pixels          = new PixelElement[_size.X, _size.Y];
        _pixelDirtyRect = new PixelDirtyRect(this);

        _viewPortSize = GetViewport().GetVisibleRect().Size;

        InitPixels();
        
        _layerManager.init(_size); 
        AddChild(_layerManager);
        _collision.Position = new Vector2I(-_size.X / 2, -_size.Y / 2);
        AddChild(_collision);

        if (DEBUG_DRAW_BORDERS)
        {
            debugBorders = new DebugImage();
            debugBorders.init(_size);
            AddChild(debugBorders);
            debugBorders.DrawBorder(new Color(1, 0, 0, 0.5f));
        }
        if (DEBUG_DRAW_PIXELS)
        {
            debugPixels = new DebugImage();
            debugPixels.init(_size);
            AddChild(debugPixels);
        }
    }

    public void init(Vector2I size, PixelWorld pixelWorld, Vector2I worldPosition)
    {
        _size = size;
        parentWorld = pixelWorld;
        this.worldPosition = worldPosition;
        
        // place the chunk in the correct position
        float pos_x = (_size.X * this.worldPosition.X) + _size.X / 2;
        float pos_y = (_size.Y * this.worldPosition.Y) + _size.Y / 2; 
        Position = new Vector2(pos_x, pos_y);
        
        //chunk.Scale = PixelSize;
        //DEBUG_RenderChunkBorder(chunk, new Color(1,0,0,0.25f));
    }

    public List<(Vector2I, Vector2I)> GetSwapPositionsDirtyRect()
    {
        foreach (Vector2I position in _pixelDirtyRect.activePixelPositions.ToArray())
        {
            int x = position.X;
            int y = position.Y;
            
            PixelElement pixel = pixels[x, y];
            
            (Vector2I current, Vector2I next) = pixel.GetSwapPosition(parentWorld, this, pixel.chunkPosition);

            if (current == next)
            {
                //_pixelDirtyRect.SetPixelInactive(current);
                continue;
            }

            _swaps.Add((current, next));
        }
        
        return _swaps;
    }

    public List<(Vector2I, Vector2I)> GetSwapPositionsAll()
    {
        Vector2I prevPosNext = new Vector2I(0, 0);
        Vector2I prevPosCurrent = new Vector2I(0, 0);
        _swaps.Clear();
        for (int y = _size.Y-1; y >= 0; y--)
        {
            for (int x = _size.X-1; x >= 0; x--)
            {
                PixelElement pixelElement = pixels[x, y];
                if (pixelElement == null) continue;

                (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(parentWorld, this, new Vector2I(x, y));
                if (current == next)
                {
                    continue; 
                }

                if (DEBUG_DRAW_PIXELS)
                {
                    // TOOD: this code is bad and i should feel bad about it, but it works somehow 
                    PixelChunk chunkCurrent = parentWorld.GetChunkFromPixelPos(current);
                    chunkCurrent?.debugPixels.ColorPixel(parentWorld.WorldToChunk(current), new Color(1,0,1,0.0f));
                    
                    PixelChunk chunkNext = parentWorld.GetChunkFromPixelPos(next);
                    chunkNext?.debugPixels.ColorPixel(parentWorld.WorldToChunk(next), new Color(0,0,1,0.25f));
                    
                    PixelChunk chunkPrevCurrent = parentWorld.GetChunkFromPixelPos(prevPosCurrent);
                    chunkPrevCurrent?.debugPixels.ColorPixel(parentWorld.WorldToChunk(prevPosCurrent), Colors.Transparent);
                    PixelChunk chunkPrevNext = parentWorld.GetChunkFromPixelPos(prevPosNext);
                    chunkPrevNext?.debugPixels.ColorPixel(parentWorld.WorldToChunk(prevPosNext), Colors.Transparent);
                }

                prevPosCurrent = current;
                //prevPosNext = next;
                _swaps.Add((current, next));
            }
        }
        
        return _swaps;
    }

    public void UpdateLayers()
    {
        _layerManager.UpdateLayers();
    }

    public void UpdatePixel(Vector2I positionInChunk, PixelElement pixel, bool isActive)
    {
        if (!IsInBound(positionInChunk)) return;
        
        //pixel.SetRandomColor();
        pixels[positionInChunk.X, positionInChunk.Y] = pixel;
        _layerManager.ColorPixel(positionInChunk, pixel);
        pixel.SetPosition_Chunk(this, positionInChunk);
        
        if (isActive)
            _pixelDirtyRect.SetPixelActive(positionInChunk);
        else
            _pixelDirtyRect.SetPixelInactive(positionInChunk);
    }

    public bool IsInBound(Vector2I positionInChunk)
    {
        return positionInChunk.X >= 0 && positionInChunk.X < _size.X && positionInChunk.Y >= 0 && positionInChunk.Y < _size.Y;
    }

    public Vector2I ToWorldPosition(Vector2I positionInChunk)
    {
        return new Vector2I(_size.X * worldPosition.X + positionInChunk.X, _size.Y * worldPosition.Y + positionInChunk.Y);
    }

    private void InitPixels()
    {
        for (int x = 0; x < _size.X; x++)
        {
            for (int y = 0; y < _size.Y; y++)
            {
                pixels[x, y] = PixelFactory.CreateAir();
                _pixelDirtyRect.SetPixelInactive(new Vector2I(x,y));
            }
        }
    }

    public void SetIsActive(bool value)
    {
        IsActive = value;
        if (DEBUG_DRAW_BORDERS) { debugBorders?.DrawBorder((IsActive) ? new Color(0,1,0,0.5f) : new Color(1,0,0,0.5f)); }
    }


    public void UpdateCollisions()
    {
        _collision.Update(_layerManager.GetLayerImage(PixelType.Solid));
    }
}