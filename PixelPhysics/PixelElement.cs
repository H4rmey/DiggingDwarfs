using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SharpDiggingDwarfs;

public abstract class PixelElement
{
    public bool IsFalling;
    public PixelState State; 
    public Color BaseColor;
    public Color Color;
    public float Mass;
    public Vector2I Velocity;
    public float Momentum;

    public PixelElement()
    {
        BaseColor = Colors.Purple;
        Color     = Colors.Purple;
        State     = PixelState.Empty; 
        IsFalling = false;
        Mass      = 0;
        Velocity  = Vector2I.Zero;
        Momentum  = 0;
    }
    
    public virtual bool IsEmpty(PixelElement element)
    {
        return element.Mass > Mass;
    }
    
    public virtual (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        return (origin, origin);
    }

    public virtual void SetRandomColor()
    {
        Color = BaseColor;
        Color addColor = new Color(
            GD.Randf()/4,
            GD.Randf()/4,
            GD.Randf()/4,
            0
        );
        Color = Color - addColor;
        if (Color.R < 0) Color.R = 0;
        if (Color.G < 0) Color.G = 0;
        if (Color.B < 0) Color.B = 0;
        if (Color.A != 1) Color.A = 1;
        
    }

    public virtual PixelElement Clone()
    {
        PixelElement clone = (PixelElement)MemberwiseClone();
        return clone;
    }

    
    public (Vector2I Current, Vector2I Next) FindNextPixelPosition(Vector2I origin, List<Vector2I> coords, PixelChunk chunk, Vector2I direction, int randomRangeOffset = 10)
    {
        // Store the first valid empty position we find
        Vector2I? firstValidPosition = null;

        foreach (Vector2I coord in coords)
        {
            Vector2I targetPos = origin + coord * direction;
            
            // Skip if position is out of bounds
            if (!chunk.IsInBounds(targetPos.X, targetPos.Y)) 
                continue;

            PixelElement pixel = chunk.pixels[targetPos.X, targetPos.Y];
            
            // If position is not empty, continue to next position
            if (!pixel.IsEmpty(this)) 
                continue;
                
            // Found a valid empty position
            firstValidPosition = targetPos;
            break; // Exit loop after finding first valid position
        }

        // Return the first valid position found, or origin if none found
        return (origin, firstValidPosition ?? origin);
    }

}
