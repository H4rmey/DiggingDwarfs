using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace SharpDiggingDwarfs;

public partial class PixelChunk : Node2D
{
    [ExportGroup("parameters")]
    [Export]
    public Vector2I Size = new Vector2I(640, 360);
    
    private Image image;
    private Vector2I mousePos;
    private Vector2 viewPortSize;
    
    private Sprite2D sprite;
    private StaticBody2D staticBody;
    
    // pdg = PixelDataGrid
    public PixelElement[,] pixels;

    //public List<(Vector2I current, Vector2I next)> Swaps;
    private ConcurrentBag<(Vector2I, Vector2I)> Swaps = new ConcurrentBag<(Vector2I, Vector2I)>();

    // !!!DEBUGGING!!!
    private Sprite2D debugSprite;
    private Image debugImage;
    // !!! BRUSH SETTINGS !!!
    private int brushSize = 8;
    private int brushColorIndex = 0;
    private PixelElement[] brushElements; 
    
    public override void _Ready()
    {
        sprite     = new Sprite2D();
        staticBody = new StaticBody2D();
        
        AddChild(sprite);
        sprite.AddChild(staticBody);

        image  = new Image();
        image  = Image.Create(Size.X, Size.Y, false, Image.Format.Rgba8);
        pixels = new PixelElement[Size.X, Size.Y];
        //Swaps   = new List<(Vector2I current, Vector2I next)>();
        
        //pixelDataGrid = InitPixedDataGrid();
        //GenerateMap();
        //RefreshChunk();
        
        viewPortSize = GetViewport().GetVisibleRect().Size;
        float width  = (int)viewPortSize.X / image.GetWidth();
        float height = (int)viewPortSize.Y / image.GetHeight();
        Scale        = new Vector2(width, height);
        Position     = new Vector2(viewPortSize.X / 2, viewPortSize.Y / 2);

        InitPixels();
        InitImage();
        
        // !!!DEBUGGING!!!
        debugSprite = new Sprite2D();
        debugImage  = new Image();
        debugImage  = Image.Create(Size.X, Size.Y, false, Image.Format.Rgba8);
        debugImage.Fill(Colors.Transparent);
        AddChild(debugSprite);
        debugSprite.Texture = ImageTexture.CreateFromImage(debugImage);
        brushElements = new PixelElement[] { new PixelAir(), new PixelSolid() , new PixelLiquid()};
    }


    public void DrawBrush()
    {
        
        // draw rectangle at positions
        debugImage.Fill(Colors.Transparent);
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (! (x == -brushSize || y == -brushSize || x == brushSize || y == brushSize) )
                {
                    continue;
                }

                if (!IsInBounds(mousePos.X+x, mousePos.Y+y))
                {
                    continue;
                }
                debugImage.SetPixel(mousePos.X + x, mousePos.Y + y, brushElements[brushColorIndex].BaseColor);
            }
        }
        debugSprite.Texture = ImageTexture.CreateFromImage(debugImage);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            // get the chunk mouse position
            Vector2 scale = new Vector2(viewPortSize.X / Size.X, viewPortSize.Y / Size.Y);
            Vector2 rawMouse = (Vector2)eventMouseMotion.Position;
            
            mousePos = new Vector2I((int)(rawMouse.X / scale.X), (int)(rawMouse.Y / scale.Y));

            DrawBrush();
        }
        
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            for (int x = -brushSize; x <= brushSize; x++)
            {
                for (int y = -brushSize; y <= brushSize; y++)
                {
                    if (!IsInBounds(mousePos.X+x, mousePos.Y+y))
                    {
                        continue;
                    }
                    
                    SetPixel(mousePos.X + x, mousePos.Y + y, brushElements[brushColorIndex].Clone());
                }
            }
        }
        else if (Input.IsMouseButtonPressed(MouseButton.Right))
        {
            for (int x = -brushSize; x <= brushSize; x++)
            {
                for (int y = -brushSize; y <= brushSize; y++)
                {
                    if (!IsInBounds(mousePos.X, mousePos.Y))
                    {
                        continue;
                    }
                    SetPixel(mousePos.X + x, mousePos.Y + y, new PixelAir().Clone());
                }
            }
        }

        if (Input.IsKeyPressed(Key.Shift))
        {
            if (Input.IsMouseButtonPressed(MouseButton.WheelDown))
            {
                brushSize = Math.Clamp(--brushSize, 1, 20);
            }
            else if (Input.IsMouseButtonPressed(MouseButton.WheelUp))
            {
                brushSize = Math.Clamp(++brushSize, 1, 20);
            }
            DrawBrush();
        }
        else
        {
            if (Input.IsMouseButtonPressed(MouseButton.WheelDown))
            {
                brushColorIndex = Math.Clamp(--brushColorIndex, 0, brushElements.Length - 1);
                DrawBrush();
            }
            else if (Input.IsMouseButtonPressed(MouseButton.WheelUp))
            {
                brushColorIndex = Math.Clamp(++brushColorIndex, 0, brushElements.Length - 1);
                DrawBrush();
            }
        }

        if (Input.IsKeyPressed(Key.Enter))
        {
            RefreshFrame();
        }
    }

    //public override void _Process(double delta)
    //{
    //    base._Process(delta);
    //    RefreshFrame();
    //}

    public override void _PhysicsProcess(double delta)
    {
        RefreshFrame();
    }
    

    private void RefreshFrame()
    {
        Swaps.Clear();

        // Parallelize the outer loop
        Parallel.For(0, Size.X, x =>
        {
            for (int y = 0; y < Size.Y; y++)
            {
                PixelElement pixelElement = pixels[x, y];
                if (pixelElement == null) continue;
                //if (!pixelElement.IsFalling) continue;
                (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(new Vector2I(x, y), this);
                if (current == next) continue;
                Swaps.Add((current, next));
            }
        });

        // Apply swaps to the image and pixels grid
        var rng = new Random();
        var swapsList = Swaps.ToList(); // Convert to list for ordering
        swapsList = swapsList.OrderBy(x => rng.Next()).ToList();
        foreach (var (current, next) in swapsList)
        {
            SwapPixels((current, next));
        }

        // Refresh the sprite texture
        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

    //private void RefreshFrame()
    //{
    //    Swaps.Clear(); 

    //    for (int x = 0; x < Size.X; x++)
    //    {
    //        for (int y = 0; y < Size.Y; y++)
    //        {
    //            PixelElement pixelElement = pixels[x, y];
    //            if (pixelElement == null) continue;
    //            //if (! pixelElement.IsFalling) continue;
    //            
    //            (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(new Vector2I(x, y), this);
    //            if (current == next) continue;
    //            
    //            Swaps.Add((current, next));
    //        }
    //    }

    //    // Apply swaps to the image and pixels grid
    //    var rng = new Random();
    //    Swaps = Swaps.OrderBy(x => rng.Next()).ToList();
    //    foreach (var (current, next) in Swaps)
    //    {
    //        SwapPixels((current, next));
    //    }

    //    // Refresh the sprite texture
    //    sprite.Texture = ImageTexture.CreateFromImage(image);
    //}

    private void InitPixels()
    {
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                pixels[x, y] = new PixelAir().Clone();
            }
        }
    }

    private void InitImage()
    {
        image.Fill(new PixelAir().Color); 
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                image.SetPixel(x,y,pixels[x, y].Color);
            }
        }
        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

    public void SwapPixels((Vector2I current, Vector2I next) swap)
    {
        int c_x = swap.current.X;
        int c_y = swap.current.Y;
        int n_x = swap.next.X;
        int n_y = swap.next.Y;
                
        // Swaps positions in the grid
        PixelElement t_cur_pix = pixels[c_x, c_y].Clone();
        PixelElement t_nxt_pix = pixels[n_x, n_y].Clone();
        
        pixels[c_x, c_y] = t_nxt_pix;
        pixels[n_x, n_y] = t_cur_pix;
       
        image.SetPixel(c_x, c_y, t_nxt_pix.Color);
        image.SetPixel(n_x, n_y, t_cur_pix.Color);
    }

    public void SetPixel(int x, int y, PixelElement pix)
    {
        if (!IsInBounds(x, y)) return;
        
        pixels[x, y] = pix;
        image.SetPixel(x,y,pix.Color);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size.X && y >= 0 && y < Size.Y;
    }
}