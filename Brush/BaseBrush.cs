using Godot;
using System.Collections.Generic;

namespace SharpDiggingDwarfs.Brush
{
    /// <summary>
    /// Base class for brush implementations with common functionality
    /// </summary>
    public abstract class BaseBrush : IBrush
    {
        public abstract string Name { get; }
        
        public abstract List<Vector2I> GetAffectedPositions(Vector2I centerPosition, int size);
        
        public abstract List<Vector2I> GetPreviewPositions(Vector2I centerPosition, int size);
        
        /// <summary>
        /// Helper method to check if a position is within bounds
        /// </summary>
        protected bool IsValidPosition(Vector2I position, Vector2I boundsSize)
        {
            return position.X >= 0 && position.X < boundsSize.X && 
                   position.Y >= 0 && position.Y < boundsSize.Y;
        }
        
        /// <summary>
        /// Helper method to filter positions by bounds
        /// </summary>
        protected List<Vector2I> FilterByBounds(List<Vector2I> positions, Vector2I boundsSize)
        {
            var filteredPositions = new List<Vector2I>();
            foreach (var pos in positions)
            {
                if (IsValidPosition(pos, boundsSize))
                {
                    filteredPositions.Add(pos);
                }
            }
            return filteredPositions;
        }
    }
}