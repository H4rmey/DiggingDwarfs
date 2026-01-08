using System.Collections.Generic;
using Godot;
using SharpDiggingDwarfs.Core.Rendering.Chunks;

namespace SharpDiggingDwarfs.Source.Core.Rendering.Chunks;

public partial class PixelDirtyRect : Node2D
{
    public List<Vector2I> ChangedPositions { get; private set; }

    private readonly List<Vector2I> _changedPositionsPrevious;
    private readonly PixelChunk _parentChunk;

    // Single dirty rect
    public Rect2I DirtyRect { get; private set; }
    public bool HasDirtyRect { get; private set; }

    public PixelDirtyRect(PixelChunk parentChunk)
    {
        _parentChunk = parentChunk;

        ChangedPositions = new List<Vector2I>();
        _changedPositionsPrevious = new List<Vector2I>();

        DirtyRect = new Rect2I();
        HasDirtyRect = false;
    }

    public void AddChangedPosition(Vector2I position)
    {
        // Prevent duplicates
        if (ChangedPositions.Contains(position))
            return;

        ChangedPositions.Add(position);
    }

    public void RemoveChangedPosition(Vector2I position)
    {
        ChangedPositions.Remove(position);
    }

    /// <summary>
    /// Rebuilds the single dirty Rect2I covering
    /// both previous and current changed positions.
    /// </summary>
    public void RecalculateDirtyRect()
    {
        HasDirtyRect = false;

        if (ChangedPositions.Count == 0 && _changedPositionsPrevious.Count == 0)
            return;

        bool initialized = false;
        Vector2I min = Vector2I.Zero;
        Vector2I max = Vector2I.Zero;

        void Expand(Vector2I pos)
        {
            if (!initialized)
            {
                min = pos;
                max = pos;
                initialized = true;
                return;
            }

            if (pos.X < min.X) min.X = pos.X;
            if (pos.Y < min.Y) min.Y = pos.Y;
            if (pos.X > max.X) max.X = pos.X;
            if (pos.Y > max.Y) max.Y = pos.Y;
        }

        // Include current frame
        foreach (var pos in ChangedPositions)
            Expand(pos);

        // Include previous frame (important for moved pixels)
        foreach (var pos in _changedPositionsPrevious)
            Expand(pos);

        // Rect2I size is exclusive → +1
        DirtyRect = new Rect2I(
            min,
            max - min + Vector2I.One
        );
        DirtyRect = DirtyRect.Grow(3);

        HasDirtyRect = true;

        // Store current as previous for next frame
        _changedPositionsPrevious.Clear();
        _changedPositionsPrevious.AddRange(ChangedPositions);
        ChangedPositions.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// Clears all tracked state (call after processing the dirty rect).
    /// </summary>
    public void Clear()
    {
        ChangedPositions.Clear();
        HasDirtyRect = false;
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!HasDirtyRect)
            return;
        
        DrawRect(
            new Rect2(DirtyRect.Position, DirtyRect.Size),
            new Color(0f, 0f, 1f, 0.3f),
            filled: true
        );
        
        // Clear after drawing to prevent artifacts when nothing is processed
        HasDirtyRect = false;
    }
}
