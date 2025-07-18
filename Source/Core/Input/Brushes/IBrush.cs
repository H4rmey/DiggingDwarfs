using Godot;
using System.Collections.Generic;

namespace SharpDiggingDwarfs.Core.Input.Brushes
{
    /// <summary>
    /// Interface for all brush implementations
    /// </summary>
    public interface IBrush
    {
        /// <summary>
        /// Gets the positions that would be affected by this brush at the given center position
        /// </summary>
        /// <param name="centerPosition">The center position of the brush</param>
        /// <param name="size">The size of the brush (radius for circle, half-width for square)</param>
        /// <returns>List of positions that would be affected</returns>
        List<Vector2I> GetAffectedPositions(Vector2I centerPosition, int size);
        
        /// <summary>
        /// Gets the preview positions for highlighting where the brush will paint
        /// </summary>
        /// <param name="centerPosition">The center position of the brush</param>
        /// <param name="size">The size of the brush</param>
        /// <returns>List of positions for the brush outline/preview</returns>
        List<Vector2I> GetPreviewPositions(Vector2I centerPosition, int size);
        
        /// <summary>
        /// The name of this brush shape
        /// </summary>
        string Name { get; }
    }
}