
using Godot;
using System;

namespace SharpDiggingDwarfs.Source.Core.Input;

public partial class Cam : Camera2D
{
    [Signal]
    public delegate void ZoomChangedEventHandler(Vector2 zoom);
    
    [Signal]
    public delegate void OffsetChangedEventHandler(Vector2 offset);
    
    public PixelWorld world;

    public override void _Ready()
    {
        ProcessCallback = Camera2DProcessCallback.Physics;
        
        Zoom = new Vector2(world.ChunkCount.X, world.ChunkCount.Y);
        Offset = new Vector2(world.WindowSize.X / 2, world.WindowSize.Y / 2);
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
        }

        if (Godot.Input.IsActionPressed("ui_up"))
        {
            Offset = new Vector2(Offset.X, Offset.Y - 1);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_down"))
        {
            Offset = new Vector2(Offset.X, Offset.Y + 1);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_left"))
        {
            Offset = new Vector2(Offset.X - 1, Offset.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_right"))
        {
            Offset = new Vector2(Offset.X + 1, Offset.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        
        if (Godot.Input.IsActionPressed("ui_page_up"))
        {
            Zoom = new Vector2(Zoom.X + 1, Zoom.Y + 1);
            EmitSignal(SignalName.ZoomChanged, Zoom);
        }
        if (Godot.Input.IsActionPressed("ui_page_down"))
        {
            Zoom = new Vector2(Zoom.X - 1, Zoom.Y - 1);
            EmitSignal(SignalName.ZoomChanged, Zoom);
        }
        
    }
}
