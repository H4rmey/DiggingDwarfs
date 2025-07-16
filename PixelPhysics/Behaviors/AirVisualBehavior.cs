using Godot;

namespace SharpDiggingDwarfs.Behaviors;

/// <summary>
/// Visual behavior for air/empty pixels
/// Based on the original PixelAir visual logic
/// </summary>
public class AirVisualBehavior : IVisualBehavior
{
    private readonly Color baseColor;

    public AirVisualBehavior(Color? baseColor = null)
    {
        this.baseColor = baseColor ?? Colors.Gray;
    }

    public Color GetBaseColor()
    {
        return baseColor;
    }

    public void SetRandomColor(PixelElementComposed pixel)
    {
        pixel.Color = baseColor;
        Color addColor = new Color(
            GD.Randf() / 10, // Very subtle variation for air
            GD.Randf() / 10,
            GD.Randf() / 10,
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
        // Air typically maintains consistent appearance
        if (pixel.Color == Colors.Transparent || pixel.Color == default(Color))
        {
            SetRandomColor(pixel);
        }
    }
}