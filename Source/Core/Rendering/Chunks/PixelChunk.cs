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

public class PixelChunk 
{
    public PixelWorld ParentWorld;
    public Vector2I Location;
    public Vector2I Size;
    
    // pdg = PixelDataGrid
    public PixelElement[,] pixels;

    //public List<(Vector2I current, Vector2I next)> Swaps;
    private ConcurrentBag<(Vector2I, Vector2I)> Swaps = new ConcurrentBag<(Vector2I, Vector2I)>();

    
    public PixelChunk(PixelWorld parentWorld, Vector2I location, Vector2I size)
    {
        ParentWorld = parentWorld;
        Location = location;
        Size = size;

        pixels = new PixelElement[size.X, size.Y];

        InitPixels();
        // Note: InitPixels() will be called after Size is set
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

    }

    public void InitPixels()
    {
        for (int x = 0; x < Size.X; x++)
        {
            for (int y = 0; y < Size.Y; y++)
            {
                pixels[x, y] = PixelFactory.CreateAir();
            }
        }
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
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size.X && y >= 0 && y < Size.Y;
    }
}