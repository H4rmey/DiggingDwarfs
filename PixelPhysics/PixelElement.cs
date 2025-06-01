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

    public virtual PixelElement Clone()
    {
        PixelElement clone = this;
        clone.Color = clone.BaseColor;
        Color addColor = new Color(
            GD.Randf()/4,
            GD.Randf()/4,
            GD.Randf()/4,
            0
        );
        clone.Color = clone.Color - addColor;
        if (clone.Color.R < 0) clone.Color.R = 0;
        if (clone.Color.G < 0) clone.Color.G = 0;
        if (clone.Color.B < 0) clone.Color.B = 0;
        if (clone.Color.A != 1) clone.Color.A = 1;
        return clone; 
    }
    
    public (Vector2I Current, Vector2I Next) FindNextPixelPosition(Vector2I origin, List<Vector2I> coords, PixelChunk chunk, Vector2I direction)
    {
        foreach (Vector2I coord in coords)
        {
            Vector2I c = origin + coord * direction;
            if (!chunk.IsInBounds(c.X, c.Y)) continue;

            PixelElement pixel = chunk.pixels[c.X, c.Y];
            if (!pixel.IsEmpty(this)) return (origin, origin); 
            
            return (origin, c);    
        }

        return (origin, origin);    
    }

}
