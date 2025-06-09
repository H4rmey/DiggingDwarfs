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
    public Vector2I Size = new Vector2I(128, 72);
    
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
    private int brushSize = 0;
    private int brushColorIndex = 1;
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
        brushElements = new PixelElement[] { 
            new PixelAir(), 
            new PixelSolid(), 
            new PixelLiquid(),
        };
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
                    SetPixel(mousePos.X + x, mousePos.Y + y, new PixelAir());
                }
            }
        }

        if (Input.IsKeyPressed(Key.Shift))
        {
            if (Input.IsMouseButtonPressed(MouseButton.WheelDown))
            {
                brushSize = Math.Clamp(--brushSize, 0, 32);
            }
            else if (Input.IsMouseButtonPressed(MouseButton.WheelUp))
            {
                brushSize = Math.Clamp(++brushSize, 0, 32);
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
            Parallel.For(0, Size.Y, y =>
            {
                PixelElement pixelElement = pixels[x, y];
                if (pixelElement == null) return;
                (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(new Vector2I(x, y), this);
                if (current == next) return;
                Swaps.Add((current, next));
            });
        });

        // Process swaps with collision handling
        ProcessSwaps();
    }

    private void ProcessSwaps()
    {
        // Track which positions are being targeted
        var targetPositions = new HashSet<Vector2I>();
        var processedSwaps = new List<(Vector2I, Vector2I)>();
        var conflictSwaps = new List<(Vector2I, Vector2I)>();
        var rng = new Random();

        // First pass: identify conflicts
        foreach (var swap in Swaps.OrderBy(x => rng.Next()))
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
            SwapPixels(swap);
        }

        // Handle conflicts in batches until no more conflicts
        while (conflictSwaps.Count > 0)
        {
            var currentConflicts = conflictSwaps;
            conflictSwaps = new List<(Vector2I, Vector2I)>();
            targetPositions.Clear();

            // Re-run GetSwapPosition for conflicting pixels
            foreach (var conflict in currentConflicts)
            {
                PixelElement pixel = pixels[conflict.Item1.X, conflict.Item1.Y];  // Item1 is the current position
                if (pixel == null) continue;

                // Get new target position
                var newSwap = pixel.GetSwapPosition(conflict.Item1, this);
                
                if (newSwap.Item1 == newSwap.Item2)  // If current == next
                {
                    // Pixel can't move, skip it
                    continue;
                }

                if (targetPositions.Add(newSwap.Item2))  // Add next position
                {
                    // No conflict with new position, process it
                    SwapPixels(newSwap);
                }
                else
                {
                    // Still has conflict, add to next batch
                    conflictSwaps.Add(newSwap);
                }
            }

            // If we have the same number of conflicts as before, we might have a deadlock
            // In this case, randomly resolve some conflicts
            if (conflictSwaps.Count >= currentConflicts.Count)
            {
                var remainingConflicts = conflictSwaps
                    .OrderBy(x => rng.Next())
                    .Take(conflictSwaps.Count / 2)
                    .ToList();

                foreach (var swap in remainingConflicts)
                {
                    SwapPixels(swap);
                }

                conflictSwaps = conflictSwaps
                    .Except(remainingConflicts)
                    .ToList();
            }
        }

        // Refresh the sprite texture
        sprite.Texture = ImageTexture.CreateFromImage(image);
    }

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
        
        pix.SetRandomColor();
        pixels[x, y] = pix;
        image.SetPixel(x,y,pix.Color);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size.X && y >= 0 && y < Size.Y;
    }
}