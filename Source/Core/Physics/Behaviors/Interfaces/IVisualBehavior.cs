using Godot;
using SharpDiggingDwarfs.Core.Physics.Elements;

namespace SharpDiggingDwarfs.Core.Physics.Behaviors.Interfaces;

/// <summary>
/// Interface for defining visual appearance and color behaviors of pixels
/// </summary>
public interface IVisualBehavior
{
    /// <summary>
    /// Gets the base color for this pixel type
    /// </summary>
    /// <returns>The base color</returns>
    Color GetBaseColor();
    
    /// <summary>
    /// Sets a randomized color variation based on the base color
    /// </summary>
    /// <param name="pixel">The pixel to apply the color to</param>
    void SetRandomColor(PixelElement pixel);
    
    /// <summary>
    /// Gets the current display color for the pixel
    /// </summary>
    /// <param name="pixel">The pixel to get the color from</param>
    /// <returns>The current color</returns>
    Color GetCurrentColor(PixelElement pixel);
    
    /// <summary>
    /// Updates the visual appearance based on pixel state (falling, momentum, etc.)
    /// </summary>
    /// <param name="pixel">The pixel to update</param>
    void UpdateVisualState(PixelElement pixel);
}