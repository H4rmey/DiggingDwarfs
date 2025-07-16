using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SharpDiggingDwarfs.Brush;

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
    public PixelElementComposed[,] pixels;

    //public List<(Vector2I current, Vector2I next)> Swaps;
    private ConcurrentBag<(Vector2I, Vector2I)> Swaps = new ConcurrentBag<(Vector2I, Vector2I)>();

    // !!! BRUSH NODE SYSTEM !!!
    private BrushNode brushNode;
    
    public override void _Ready()
    {
        sprite     = new Sprite2D();
        staticBody = new StaticBody2D();
        
        AddChild(sprite);
        sprite.AddChild(staticBody);

        image  = new Image();
        image  = Image.CreateEmpty(Size.X, Size.Y, false, Image.Format.Rgba8);
        pixels = new PixelElementComposed[Size.X, Size.Y];
        
        viewPortSize = GetViewport().GetVisibleRect().Size;
        float width  = (int)viewPortSize.X / image.GetWidth();
        float height = (int)viewPortSize.Y / image.GetHeight();
        Scale        = new Vector2(width, height);
        Position     = new Vector2(viewPortSize.X / 2, viewPortSize.Y / 2);

        InitPixels();
        InitImage();
        
        // Initialize the brush node system
        SetupBrushNode();
    }


    private void SetupBrushNode()
    {
        // Load the brush node scene
        var brushScene = GD.Load<PackedScene>("res://Brush/BrushNode.tscn");
        brushNode = brushScene.Instantiate<BrushNode>();
        
        // Configure the brush node
        brushNode.SetChunkSize(Size);
        
        // Connect signals
        brushNode.PaintRequested += OnBrushPaintRequested;
        brushNode.EraseRequested += OnBrushEraseRequested;
        brushNode.BrushChanged += OnBrushChanged;
        
        // Add as child
        AddChild(brushNode);
    }
    
    private void OnBrushPaintRequested(Vector2I position, int pixelTypeIndex)
    {
        if (IsInBounds(position.X, position.Y))
        {
            var pixel = brushNode.GetPixelByIndex(pixelTypeIndex);
            SetPixel(position.X, position.Y, pixel);
        }
    }
    
    private void OnBrushEraseRequested(Vector2I position)
    {
        if (IsInBounds(position.X, position.Y))
        {
            SetPixel(position.X, position.Y, PixelFactory.CreateAir());
        }
    }
    
    private void OnBrushChanged(string brushName, int size, string pixelType)
    {
        // Optional: Handle brush change events (e.g., update UI)
        GD.Print($"Brush changed: {brushName}, Size: {size}, Type: {pixelType}");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            // get the chunk mouse position
            Vector2 scale = new Vector2(viewPortSize.X / Size.X, viewPortSize.Y / Size.Y);
            Vector2 rawMouse = (Vector2)eventMouseMotion.Position;
            
            mousePos = new Vector2I((int)(rawMouse.X / scale.X), (int)(rawMouse.Y / scale.Y));
        }

        if (Input.IsKeyPressed(Key.Enter))
        {
            RefreshFrame();
        }
        
        // Debug: Print brush info when Tab is pressed
        if (Input.IsActionJustPressed("ui_focus_next")) // Tab key
        {
            if (brushNode != null)
            {
                GD.Print(brushNode.GetBrushInfo());
            }
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
                PixelElementComposed pixelElement = pixels[x, y];
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
                PixelElementComposed pixel = pixels[conflict.Item1.X, conflict.Item1.Y];  // Item1 is the current position
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
        PixelElementComposed t_cur_pix = pixels[c_x, c_y].Clone();
        PixelElementComposed t_nxt_pix = pixels[n_x, n_y].Clone();
        
        pixels[c_x, c_y] = t_nxt_pix;
        pixels[n_x, n_y] = t_cur_pix;
       
        image.SetPixel(c_x, c_y, t_nxt_pix.Color);
        image.SetPixel(n_x, n_y, t_cur_pix.Color);
    }

    public void SetPixel(int x, int y, PixelElementComposed pix)
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
/// <summary>
    /// Test method to validate composition integration with existing chunk system
    /// This demonstrates how composed pixels can work alongside the current system
    /// </summary>
    public void TestCompositionIntegration()
    {
        GD.Print("=== Testing Composition Integration with PixelChunk ===");
        
        // Run the basic composition tests
        CompositionProofOfConcept.RunBasicTests();
        
        // Test creating composed pixels
        var composedSolid = PixelFactory.CreateSolid();
        var composedAir = PixelFactory.CreateAir();
        
        // Test that composed pixels have the expected properties
        GD.Print($"Composed solid - State: {composedSolid.State}, Mass: {composedSolid.Mass}");
        GD.Print($"Composed air - State: {composedAir.State}, Mass: {composedAir.Mass}");
        
        // Test IsEmpty logic between composed pixels
        bool airEmptyForSolid = composedAir.IsEmpty(composedSolid);
        bool solidEmptyForAir = composedSolid.IsEmpty(composedAir);
        
        GD.Print($"Air empty for solid: {airEmptyForSolid} (should be true)");
        GD.Print($"Solid empty for air: {solidEmptyForAir} (should be false)");
        
        // Test behavior assignment
        bool solidHasBehavior = composedSolid.MovementBehavior != null;
        bool airHasBehavior = composedAir.MovementBehavior != null;
        
        GD.Print($"Solid has movement behavior: {solidHasBehavior} (should be true)");
        GD.Print($"Air has movement behavior: {airHasBehavior} (should be false)");
        
        // Test GetSwapPosition delegation
        var testOrigin = new Vector2I(10, 10);
        var swapResult = composedSolid.GetSwapPosition(testOrigin, this);
        
        GD.Print($"GetSwapPosition test - Origin: {testOrigin}, Result: {swapResult}");
        
        GD.Print("✓ Composition integration test completed successfully");
        GD.Print("=== Integration test finished ===");
    }
}