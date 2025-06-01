using Godot;

namespace SharpDiggingDwarfs;

public class PixelAir : PixelElement
{
    public PixelAir()
    {
        BaseColor = Colors.Gray; 
        Color     = Colors.LightGray; 
        State = PixelState.Empty; // Sand behaves like a solid granular material
        IsFalling = false; // Sand falls by default
        Mass = 0;
    }

    public override (Vector2I Current, Vector2I Next) GetSwapPosition(Vector2I origin, PixelChunk chunk)
    {
        return (origin, origin);
    }
    
    public override PixelElement Clone()
    {
        PixelElement clone = this;
        clone.Color = clone.BaseColor;
        Color addColor = new Color(
            GD.Randf()/10,
            GD.Randf()/10,
            GD.Randf()/10,
            0
        );
        clone.Color = clone.Color - addColor;
        if (clone.Color.R < 0) clone.Color.R = 0;
        if (clone.Color.G < 0) clone.Color.G = 0;
        if (clone.Color.B < 0) clone.Color.B = 0;
        if (clone.Color.A != 1) clone.Color.A = 1;
        return clone; 
    }

}
