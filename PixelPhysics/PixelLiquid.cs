using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Godot;

namespace SharpDiggingDwarfs;
public class PixelLiquid : PixelElement
{
    public float Viscocity;
    public PixelLiquid()
    {
        BaseColor = Colors.Blue; 
        Color     = Colors.LightBlue; 
        State     = PixelState.Liquid; 
        IsFalling = true;
        Mass      = 0.2f;
        Velocity  = Vector2I.Zero;
        Momentum  = 0;
        
        // custom for liquid only
        Viscocity = 8;
    }

    public override (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        int x = origin.X;
        int y = origin.Y;

        // First only check the pixel below
        if (chunk.IsInBounds(origin.X, origin.Y + 1))
        {
            PixelElement pixel = chunk.pixels[origin.X, origin.Y + 1];
            if (pixel.IsEmpty(this))
            {
                return (origin, new Vector2I(origin.X, origin.Y + 1));    
            }
        }

            
        
        // If you cannot go below, go to the side instead 
        List<Vector2I> coords = new List<Vector2I>();
        for (int i = 1; i < Viscocity; i++)
        {
            coords.Add(new Vector2I(i, 0));
        }

        bool doLeftFirst = GD.RandRange(0, 1)==0;
        Vector2I direction = (doLeftFirst) ? Vector2I.Left : Vector2I.Right;
        
        (Vector2I Current, Vector2I Next) = FindNextPixelPosition(origin, coords, chunk, direction);
        if (Current != Current) return (Current, Next);
        
        direction = (!doLeftFirst) ? Vector2I.Left : Vector2I.Right;
        (Current, Next) = FindNextPixelPosition(origin, coords, chunk, direction);
        
        return (Current, Next);
    }
}
