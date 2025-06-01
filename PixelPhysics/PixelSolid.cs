using System.Collections.Generic;
using Godot;

namespace SharpDiggingDwarfs;
public class PixelSolid : PixelElement
{
    public PixelSolid()
    {
        BaseColor = Colors.Yellow; 
        Color     = Colors.Orange; 
        State     = PixelState.Solid; 
        IsFalling = true;
        Mass      = 0.5f;
        Velocity  = Vector2I.Zero;
        Momentum  = 0;
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
        for (int i = 1; i < 4; i++)
        {
            coords.Add(new Vector2I(i, 1));
        }

        bool doLeftFirst = GD.RandRange(0, 1)==0;
        Vector2I direction = (doLeftFirst) ? new Vector2I(-1,1) : new Vector2I(1,1);
        
        (Vector2I Current, Vector2I Next) = FindNextPixelPosition(origin, coords, chunk, direction);
        if (Current != Current) return (Current, Next);
        
        direction = (doLeftFirst) ? new Vector2I(-1,1) : new Vector2I(1,1);
        (Current, Next) = FindNextPixelPosition(origin, coords, chunk, direction);
        
        return (Current, Next);
    }
}
