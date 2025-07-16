using Godot;
using System.Collections.Generic;

namespace SharpDiggingDwarfs.Brush
{
    /// <summary>
    /// Square-shaped brush implementation
    /// </summary>
    public class SquareBrush : BaseBrush
    {
        public override string Name => "Square";
        
        public override List<Vector2I> GetAffectedPositions(Vector2I centerPosition, int size)
        {
            var positions = new List<Vector2I>();
            
            // For size 0, just return the center position
            if (size == 0)
            {
                positions.Add(centerPosition);
                return positions;
            }
            
            // Generate all positions within the square
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    positions.Add(new Vector2I(centerPosition.X + x, centerPosition.Y + y));
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
            
            // Generate square outline - only the border pixels
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    // Include only border positions
                    bool isOnBorder = (x == -size || x == size || y == -size || y == size);
                    
                    if (isOnBorder)
                    {
                        positions.Add(new Vector2I(centerPosition.X + x, centerPosition.Y + y));
                    }
                }
            }
            
            return positions;
        }
    }
}