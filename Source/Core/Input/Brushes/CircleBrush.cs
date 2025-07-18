using Godot;
using System.Collections.Generic;

namespace SharpDiggingDwarfs.Core.Input.Brushes
{
    /// <summary>
    /// Circle-shaped brush implementation
    /// </summary>
    public class CircleBrush : BaseBrush
    {
        public override string Name => "Circle";
        
        public override List<Vector2I> GetAffectedPositions(Vector2I centerPosition, int size)
        {
            var positions = new List<Vector2I>();
            
            // For size 0, just return the center position
            if (size == 0)
            {
                positions.Add(centerPosition);
                return positions;
            }
            
            // Generate all positions within the circle
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    // Check if the position is within the circle using distance formula
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance <= size)
                    {
                        positions.Add(new Vector2I(centerPosition.X + x, centerPosition.Y + y));
                    }
                }
            }
            
            return positions;
        }
        
        public override List<Vector2I> GetPreviewPositions(Vector2I centerPosition, int size)
        {
            var positions = new List<Vector2I>();
            
            // For size 0, just return the center position
            if (size == 0)
            {
                positions.Add(centerPosition);
                return positions;
            }
            
            // Generate circle outline using Bresenham-like algorithm
            // We'll create a thick outline by including positions at distance size-0.5 to size+0.5
            for (int x = -size - 1; x <= size + 1; x++)
            {
                for (int y = -size - 1; y <= size + 1; y++)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    
                    // Include positions that are on the edge of the circle
                    if (distance >= size - 0.5f && distance <= size + 0.5f)
                    {
                        positions.Add(new Vector2I(centerPosition.X + x, centerPosition.Y + y));
                    }
                }
            }
            
            // If no outline positions found (very small circle), fall back to affected positions
            if (positions.Count == 0)
            {
                return GetAffectedPositions(centerPosition, size);
            }
            
            return positions;
        }
    }
}