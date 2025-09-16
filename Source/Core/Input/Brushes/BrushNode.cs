using Godot;
using System.Collections.Generic;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using GodotInput = Godot.Input;

namespace SharpDiggingDwarfs.Core.Input.Brushes
{
    /// <summary>
    /// A Godot Node2D that provides brush functionality for painting pixels
    /// Can be used by player characters, UI systems, or any other game object
    ///
    /// Keyboard Controls:
    /// - Number keys 0-9: Select pixel type directly
    /// - C key: Switch to Circle brush
    /// - S key: Switch to Square brush
    /// - + key: Increase brush size
    /// - - key: Decrease brush size
    ///
    /// Mouse Controls (Alternative):
    /// - Mouse Wheel: Change pixel type
    /// - Shift + Mouse Wheel: Change brush size
    /// - Ctrl + Mouse Wheel: Change brush shape
    /// </summary>
    public partial class BrushNode : Node2D
    {
        [Signal]
        public delegate void BrushChangedEventHandler(string brushName, int size, string pixelType);
        
        [Signal]
        public delegate void PaintRequestedEventHandler(Vector2I pos, int pixelTypeIndex);
        
        [Signal]
        public delegate void EraseRequestedEventHandler(Vector2I pos);
        
        private BrushManager brushManager;
        public Sprite2D previewSprite;
        private Image previewImage;
        public PixelWorld ParentWorld;
        
        private Vector2 mousePos;
        private bool isHeldDown;
        
        
        public override void _Ready()
        {
            previewSprite = new Sprite2D();
            previewImage = Image.CreateEmpty((int)ParentWorld.WorldSize.X, (int)ParentWorld.WorldSize.Y, false, Image.Format.Rgba8);
            previewImage.Fill(Colors.Transparent);

            Scale = ParentWorld.PixelSize; 
            Position = new Vector2(ParentWorld.WindowSize.X/2, ParentWorld.WindowSize.Y/2);
            
            AddChild(previewSprite);
            previewSprite.Texture = ImageTexture.CreateFromImage(previewImage);
        }
        
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (isHeldDown)
            {
                EmitSignal(SignalName.PaintRequested, mousePos, 0);
            }
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion eventMouseMotion)
            {
                mousePos = eventMouseMotion.Position;
            }
            
            // Debug: Print brush info when Tab is pressed
            if (!Godot.Input.IsMouseButtonPressed(MouseButton.Left))
            {
                isHeldDown = false;
                //EmitSignal(SignalName.PaintRequested, mousePos, 0);
            }
        
            // Debug: Print brush info when Tab is pressed
            if (Godot.Input.IsMouseButtonPressed(MouseButton.Left))
            {
                isHeldDown = true;
            }
        }
    }
}