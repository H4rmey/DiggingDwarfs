using Godot;

namespace SharpDiggingDwarfs.Behaviors;

/// <summary>
/// Visual behavior for solid particles
/// Based on the original PixelSolid visual logic
/// </summary>
public class SolidVisualBehavior : IVisualBehavior
{
    private readonly Color baseColor;

    public SolidVisualBehavior(Color? baseColor = null)
    {
        this.baseColor = baseColor ?? Colors.Yellow;
    }

    public Color GetBaseColor()
    {
        return baseColor;
    }

    public void SetRandomColor(PixelElementComposed pixel)
    {
        pixel.Color = baseColor;
        Color addColor = new Color(
            GD.Randf() / 4,
            GD.Randf() / 4,
            GD.Randf() / 4,
            0
        );
        pixel.Color = pixel.Color - addColor;
        
        // Clamp color values
        if (pixel.Color.R < 0) pixel.Color.R = 0;
        if (pixel.Color.G < 0) pixel.Color.G = 0;
        if (pixel.Color.B < 0) pixel.Color.B = 0;
        if (pixel.Color.A != 1) pixel.Color.A = 1;
    }

    public Color GetCurrentColor(PixelElementComposed pixel)
    {
        return pixel.Color;
    }

    public void UpdateVisualState(PixelElementComposed pixel)
    {
        // Could add visual effects based on falling state, momentum, etc.
        // For now, keep the basic color
        if (pixel.Color == Colors.Transparent || pixel.Color == default(Color))
        {
            SetRandomColor(pixel);
        }
    }
}