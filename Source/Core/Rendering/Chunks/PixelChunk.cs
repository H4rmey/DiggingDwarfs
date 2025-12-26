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

namespace SharpDiggingDwarfs.Core.Rendering.Chunks;

public partial class PixelChunk : Node2D
{
    public Vector2I Size = new Vector2I(32, 18);
    public Vector2I WorldPosition;
    public PixelWorld ParentWorld;
    
    public Image image;
    public Sprite2D sprite;
    public ImageTexture texture;
    public Vector2I mousePos;
    public Vector2 viewPortSize;
    
    public StaticBody2D staticBody;
    
    // pdg = PixelDataGrid
    public PixelElement[,] pixels;

    public List<(Vector2I, Vector2I)> Swaps = new();

    private const bool DEBUG_DRAW_BORDERS = true;
    private DebugImage debugBorders;
    
    private const bool DEBUG_DRAW_PIXELS = true;
    public DebugImage debugPixels;

    private bool IsActive = true;
    
    public override void _Ready()
    {
        staticBody = new StaticBody2D();
        sprite     = new Sprite2D();
        image      = new Image();
        pixels     = new PixelElement[Size.X, Size.Y];
        
        AddChild(sprite);
        sprite.AddChild(staticBody);

        image = Image.CreateEmpty(Size.X, Size.Y, false, Image.Format.Rgba8);
        
        viewPortSize = GetViewport().GetVisibleRect().Size;

        InitPixels();
        InitImage();

        if (DEBUG_DRAW_BORDERS)
        {
            debugBorders = new DebugImage();
            debugBorders.init(Size);
            AddChild(debugBorders);
            debugBorders.DrawBorder(new Color(1, 0, 0, 0.5f));
        }
        if (DEBUG_DRAW_PIXELS)
        {
            debugPixels = new DebugImage();
            debugPixels.init(Size);
            AddChild(debugPixels);
        }
    }
    
    public List<(Vector2I, Vector2I)> GetSwapPositions()
    {
        Vector2I prevPosNext = new Vector2I(0, 0);
        Vector2I prevPosCurrent = new Vector2I(0, 0);
        Swaps.Clear();
        for (int y = Size.Y-1; y >= 0; y--)
        {
            for (int x = Size.X-1; x >= 0; x--)
            {
                PixelElement pixelElement = pixels[x, y];
                if (pixelElement == null) continue;

                (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(ParentWorld, this, new Vector2I(x, y));
                if (current == next)
                {
                    continue; 
                }

                if (DEBUG_DRAW_PIXELS)
                {
                    // TOOD: this code is bad and i should feel bad about it, but it works somehow 
                    PixelChunk chunkCurrent = ParentWorld.GetChunkFrom(current);
                    chunkCurrent?.debugPixels.ColorPixel(ParentWorld.WorldToChunk(current), new Color(1,0,1,0.0f));
                    
                    PixelChunk chunkNext = ParentWorld.GetChunkFrom(next);
                    chunkNext?.debugPixels.ColorPixel(ParentWorld.WorldToChunk(next), new Color(0,0,1,0.25f));
                    
                    PixelChunk chunkPrevCurrent = ParentWorld.GetChunkFrom(prevPosCurrent);
                    chunkPrevCurrent?.debugPixels.ColorPixel(ParentWorld.WorldToChunk(prevPosCurrent), Colors.Transparent);
                    PixelChunk chunkPrevNext = ParentWorld.GetChunkFrom(prevPosNext);
                    chunkPrevNext?.debugPixels.ColorPixel(ParentWorld.WorldToChunk(prevPosNext), Colors.Transparent);
                }

                prevPosCurrent = current;
                //prevPosNext = next;
                Swaps.Add((current, next));
            }
        }
        
        return Swaps;
    }
    public Vector2I ToWorldPosition(Vector2I pos)
    {
        return new Vector2I(Size.X * WorldPosition.X + pos.X, Size.Y * WorldPosition.Y + pos.Y);
    }

    public void SetPixel(Vector2I pos, PixelElement pix)
    {
        if (!IsInBound(pos)) return;
        
        pix.SetRandomColor();
        pixels[pos.X, pos.Y] = pix;
        image.SetPixelv(pos, pix.Color);
    }

    public bool IsInBound(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < Size.X && pos.Y >= 0 && pos.Y < Size.Y;
    }

    private void InitPixels()
    {
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                pixels[x, y] = PixelFactory.CreateAir();
            }
        }
    }

    private void InitImage()
    {
        image.Fill(PixelFactory.CreateAir().Color);
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                image.SetPixel(x, y, pixels[x, y].Color);
            }
        }
        texture = ImageTexture.CreateFromImage(image);
        sprite.Texture = texture;
    }

    public void SetIsActive(bool value)
    {
        IsActive = value;
        if (DEBUG_DRAW_BORDERS) { debugBorders?.DrawBorder((IsActive) ? new Color(0,1,0,0.5f) : new Color(1,0,0,0.5f)); }
    }
}