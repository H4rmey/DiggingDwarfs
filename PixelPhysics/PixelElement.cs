using System.Collections.Generic;
using System.Linq;
using Godot;
using System;

namespace SharpDiggingDwarfs;

public abstract class PixelElement
{
    public delegate void PixelAction(PixelElement pixel, Vector2I position);
    
    public bool IsFalling;
    public PixelState State; 
    public Color BaseColor;
    public Color Color;
    public float Mass;
    public Vector2I Velocity;
    public float Momentum;
    public float Friction;
    public bool SuddenStop = false;
    public Vector2I MomentumDirection = Vector2I.Zero; // Store the direction of momentum

    public PixelElement()
    {
        BaseColor = Colors.Purple;
        Color     = Colors.Purple;
        State     = PixelState.Empty; 
        IsFalling = false;
        Mass      = 0;
        Velocity  = Vector2I.Zero;
        Momentum  = 0;
        Friction  = (float)GD.RandRange(0.0f, 1.0f);
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

    public virtual bool SetSuddenStop()
    {
        if (GD.RandRange(0.0f, 1.0f) < Friction)
        {
            SuddenStop = true;
            Momentum = 0.0f;
            MomentumDirection = Vector2I.Zero; // Reset direction when momentum is used up
            return true;
        }
        return false;
    }

    public virtual void CheckSurroundingPixels(Vector2I origin, PixelChunk chunk, PixelAction action)
    {
        // Define all 8 surrounding positions (including diagonals)
        Vector2I[] surroundingPositions = new Vector2I[]
        {
            new Vector2I(-1, -1), // top-left
            new Vector2I(0, -1),  // top
            new Vector2I(1, -1),  // top-right
            new Vector2I(-1, 0),  // left
            new Vector2I(1, 0),   // right
            new Vector2I(-1, 1),  // bottom-left
            new Vector2I(0, 1),   // bottom
            new Vector2I(1, 1)    // bottom-right
        };

        foreach (Vector2I offset in surroundingPositions)
        {
            Vector2I checkPos = origin + offset;
            
            // Skip if position is out of bounds
            if (!chunk.IsInBounds(checkPos.X, checkPos.Y))
                continue;

            // Get the pixel at this position
            PixelElement pixel = chunk.pixels[checkPos.X, checkPos.Y];
            
            // Execute the provided action on this pixel
            action(pixel, checkPos);
        }
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
