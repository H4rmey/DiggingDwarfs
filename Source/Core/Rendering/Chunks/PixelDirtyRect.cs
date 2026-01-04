using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Source.Core.Rendering.Chunks;

public partial class PixelDirtyRect : Node2D
{
    //public List<PixelElement> activePixels { get; private set; }
    public List<Vector2I> activePixelPositions { get; private set; }
    private PixelChunk _parentChunk;
    
    public List<Rect2I> _rects { get; private set; }
    
    private Vector2I lowest;
    private Vector2I highest;
    
    public PixelDirtyRect(PixelChunk parentChunk)
    {
        //activePixels = new List<PixelElement>(); 
        activePixelPositions = new List<Vector2I>(); 
        _parentChunk = parentChunk; 
    }
    
    public void SetPixelActive(Vector2I worldPosition)
    {
        //lowest.X = (lowest.X <= chunkPosition.X) ? lowest.X : chunkPosition.X;
        //lowest.Y = (lowest.Y <= chunkPosition.Y) ? lowest.Y : chunkPosition.Y;
        //highest.X = (highest.X >= chunkPosition.X) ? highest.X : chunkPosition.X;
        //highest.Y = (highest.Y >= chunkPosition.Y) ? highest.Y : chunkPosition.Y;

        if (activePixelPositions.Contains(worldPosition))
            return; 
        
        //activePixels.Add(pixel);
        activePixelPositions.Add(worldPosition);
        _parentChunk.pixels[worldPosition.X, worldPosition.Y].IsActive = true;
        GD.Print("Added pixel: ", worldPosition);
    }

    public void SetPixelInactive(Vector2I worldPosition)
    {
        if (!activePixelPositions.Contains(worldPosition))
            return;
            
        //activePixels.Remove(pixel);
        activePixelPositions.Remove(worldPosition);
        _parentChunk.pixels[worldPosition.X, worldPosition.Y].IsActive = false;
        GD.Print("Removed pixel: ", worldPosition);
    }

    //public List<(Vector2I, Vector2I)> UpdateActivePixels()
    //{
    //    List<(Vector2I, Vector2I)> swaps = new();
    //    
    //    foreach (PixelElement p in activePixels)
    //    {
    //        if (p.IsActive)
    //        {
    //            swaps.Add(p.GetSwapPosition(_parentChunk.parentWorld, _parentChunk, p.chunkPosition));
    //        }
    //        else
    //        {
    //            SetPixelInactive(p);
    //        }
    //    }
    //    
    //    return swaps; 
    //}
}
