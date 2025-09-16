
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

    [Export] public Vector2 PanSpeed = new Vector2(20,20);
    [Export] public Vector2 ZoomSteps = new Vector2(0.5f, 0.5f);

    public override void _Ready()
    {
        ProcessCallback = Camera2DProcessCallback.Physics;
        
        //Zoom = new Vector2(world.ChunkCount.X, world.ChunkCount.Y);
        //Offset = new Vector2(world.WorldSize.X / 2, world.WorldSize.Y / 2);
    }
    
    public override void _Input(InputEvent @event)
{
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
        }

        if (Godot.Input.IsActionPressed("ui_up"))
        {
            Offset = new Vector2(Offset.X, Offset.Y + PanSpeed.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_down"))
        {
        Offset = new Vector2(Offset.X, Offset.Y - PanSpeed.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_left"))
        {
            Offset = new Vector2(Offset.X + PanSpeed.X, Offset.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        if (Godot.Input.IsActionPressed("ui_right"))
        {
            Offset = new Vector2(Offset.X - PanSpeed.X, Offset.Y);
            EmitSignal(SignalName.OffsetChanged, Offset);
        }
        
        if (Godot.Input.IsActionPressed("ui_page_up"))
        {
            Zoom = new Vector2(Zoom.X + ZoomSteps.X, Zoom.Y + ZoomSteps.Y);
            EmitSignal(SignalName.ZoomChanged, Zoom);
        }
        if (Godot.Input.IsActionPressed("ui_page_down"))
        {
            Zoom = new Vector2(Zoom.X - ZoomSteps.X, Zoom.Y - ZoomSteps.Y);
            EmitSignal(SignalName.ZoomChanged, Zoom);
        }
        
    }
}
