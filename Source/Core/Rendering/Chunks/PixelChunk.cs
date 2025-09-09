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
    public PixelWorld ParentWorld;
    public Vector2I Location;
    public Vector2I Size;
    
    // pdg = PixelDataGrid
    public PixelElement[,] pixels;

    // Rendering components
    private Image chunkImage;
    private Sprite2D chunkSprite;
    
    //public List<(Vector2I current, Vector2I next)> Swaps;
    private ConcurrentBag<(Vector2I, Vector2I)> Swaps = new ConcurrentBag<(Vector2I, Vector2I)>();

    public bool IsActive = true;
    public bool IsDirty = true; // Track if chunk needs visual update
    public bool ForceUpdate = false; // Force update even if not dirty
    public bool BorderStateChanged = true; // Track if border state changed
    
    public PixelChunk(PixelWorld parentWorld, Vector2I location, Vector2I size)
    {
        ParentWorld = parentWorld;
        Location = location;
        Size = size;

        pixels = new PixelElement[size.X, size.Y];
        
        // Initialize rendering components
        chunkImage = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        chunkSprite = new Sprite2D();
        
        // Set up sprite properties
        chunkSprite.Texture = ImageTexture.CreateFromImage(chunkImage);

        InitPixels();
        // Note: InitPixels() will be called after Size is set
    }


    /// <summary>
    /// Calculates swap positions for all pixels in this chunk and returns them as chunk coordinates
    /// </summary>
    /// <returns>List of swap pairs in chunk coordinates</returns>
    public List<(Vector2I current, Vector2I next)> CalculateSwapPositions()
    {
        var swaps = new ConcurrentBag<(Vector2I, Vector2I)>();

        // Use single Parallel.For for better performance - avoid nested parallelism
        // Use thread-local storage to reduce locking overhead
        Parallel.For(0, Size.X * Size.Y, index =>
        {
            int x = index % Size.X;
            int y = index / Size.X;

            PixelElement pixelElement = pixels[x, y];
            if (pixelElement == null) return;
            
            // Get swap position in chunk coordinates
            Vector2I chunkPos = new Vector2I(x, y);
            (Vector2I current, Vector2I next) = pixelElement.GetSwapPosition(chunkPos, this, ParentWorld);
            if (current == next) return;

            swaps.Add((current, next));
        });

        return swaps.ToList();
    }

    /// <summary>
    /// Swaps pixels within this chunk using chunk-local coordinates
    /// </summary>
    /// <param name="swap">Swap pair in chunk-local coordinates</param>
    public void SwapPixelsLocal((Vector2I current, Vector2I next) swap)
    {
        int c_x = swap.current.X;
        int c_y = swap.current.Y;
        int n_x = swap.next.X;
        int n_y = swap.next.Y;

        // Bounds check for chunk coordinates
        if (!IsInBounds(c_x, c_y) || !IsInBounds(n_x, n_y)) return;
                
        // Swaps positions in the grid
        PixelElement t_cur_pix = pixels[c_x, c_y].Clone();
        PixelElement t_nxt_pix = pixels[n_x, n_y].Clone();
        
        pixels[c_x, c_y] = t_nxt_pix;
        pixels[n_x, n_y] = t_cur_pix;
    }

    public void InitPixels()
    {
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                pixels[x, y] = PixelFactory.CreateAir();
                // Update the chunk image with the initial pixel color
                chunkImage.SetPixel(x, y, pixels[x, y].Color);
            }
        }
        
        // Update the sprite texture after initializing all pixels
        chunkSprite.Texture = ImageTexture.CreateFromImage(chunkImage);
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
    }

    public void SetPixel(int x, int y, PixelElement pix)
    {
        if (!IsInBounds(x, y)) return;
        
        pix.SetRandomColor();
        pixels[x, y] = pix;
        
        // Update the chunk image immediately
        chunkImage.SetPixel(x, y, pix.Color);
        chunkSprite.Texture = ImageTexture.CreateFromImage(chunkImage);
        
        // Mark as dirty for potential physics updates
        IsDirty = true;
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size.X && y >= 0 && y < Size.Y;
    }
    
    /// <summary>
    /// Updates the visual representation of this chunk
    /// </summary>
    public void UpdateVisuals()
    {
        if (!IsDirty && !ForceUpdate) return;
        
        // Update all pixels in the chunk image
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                if (pixels[x, y] != null)
                {
                    chunkImage.SetPixel(x, y, pixels[x, y].Color);
                }
            }
        }
        
        // Update the sprite texture
        chunkSprite.Texture = ImageTexture.CreateFromImage(chunkImage);
        
        // Reset flags
        IsDirty = false;
        ForceUpdate = false;
    }
    
    /// <summary>
    /// Gets the sprite for this chunk
    /// </summary>
    public Sprite2D GetSprite()
    {
        return chunkSprite;
    }
    
    /// <summary>
    /// Gets the world position of this chunk
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        return new Vector2(Location.X * Size.X, Location.Y * Size.Y);
    }
    
}