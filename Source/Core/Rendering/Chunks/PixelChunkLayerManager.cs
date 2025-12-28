using System;
using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using SharpDiggingDwarfs.Source.Core.Rendering;
using SharpDiggingDwarfs.Source.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Core.Rendering.Chunks;

public partial class PixelChunkLayerManager : Node2D
{
    private Vector2I _size = new Vector2I(32, 18);
    private PixelChunkLayer[] _layers;

    public void init(Vector2I size)
    {
        _size = size;
        
        _layers = new PixelChunkLayer[Enum.GetValues<PixelType>().Length];

        foreach (PixelType type in Enum.GetValues<PixelType>())
        {
            _layers[(int)type] = new PixelChunkLayer();
            _layers[(int)type].init(_size);
            AddChild(_layers[(int)type]);
        }
    }

    private void InitLayersEmpty()
    {
        foreach (PixelType type in Enum.GetValues<PixelType>())
        {
            _layers[(int)type].InitEmpty();
        }
    }

    public void ColorPixel(Vector2I pos, PixelElement pixel)
    {
        PixelChunkLayer layer = _layers[(int)pixel.Behaviour.Type];

        if (!layer.IsInBound(pos)) return;
       
        // TODO: this might be slow
        foreach (PixelChunkLayer l in _layers)
        {
            l.ColorPixel(pos, Colors.Transparent); 
        }
        layer.ColorPixel(pos, pixel.Color); 
    }

    public void UpdateLayers()
    {
        foreach (PixelType type in Enum.GetValues<PixelType>())
        {
            _layers[(int)type].Update();
        }
    }

    public Image GetLayerImage(PixelType type) { return _layers[(int)type].image; }
}
