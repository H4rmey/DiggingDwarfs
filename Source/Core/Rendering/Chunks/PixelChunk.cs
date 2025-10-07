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
    
    public bool IsActive = true;
    
    public bool Debug = true;
    public Image debugImage;
    public Sprite2D debugSprite;
    public ImageTexture debugTexture;
    
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
        DEBUG_init();
    }
    
    public List<(Vector2I, Vector2I)> GetSwapPositions()
    {
        Swaps.Clear();
        for (int y = Size.Y-1; y >= 0; y--)
        {
            for (int x = Size.X-1; x >= 0; x--)
            {
                PixelElement pixelElement = pixels[x, y];
                if (pixelElement == null) continue;

                (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(ParentWorld, this, new Vector2I(x, y));
                if (current == next) continue;

                Swaps.Add((current, next));
            }
        }

        return Swaps;
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

    private void DEBUG_init()
    {
        debugSprite = new Sprite2D();
        debugImage  = new Image();
        
        debugImage = Image.CreateEmpty(Size.X, Size.Y, false, Image.Format.Rgba8);
        debugImage.Fill(Colors.Transparent);
        debugTexture = ImageTexture.CreateFromImage(debugImage);
        debugSprite.Texture = debugTexture;
        AddChild(debugSprite);
    }

    public void DEBUG_DrawBorder(Color color)
    {
        for (int x = 0; x < debugImage.GetSize().X; x++)
        {
            for (int y = 0; y < debugImage.GetSize().Y; y++)
            {
                if (x == 0 || y == 0 || x == Size.X - 1 || y == Size.Y - 1)
                {
                    debugImage.SetPixel(x, y, color);
                }
            }
        }
        debugTexture.Update(debugImage);
    }
}