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
        public delegate void PaintRequestedEventHandler(Vector2I pos, int pixelTypeIndex, int brushSize);
        
        [Signal]
        public delegate void EraseRequestedEventHandler(Vector2I pos, int brushSize);
        
        public PixelWorld ParentWorld;
        
        private BrushManager brushManager;
        private Image previewImage;
        private Sprite2D previewSprite;

        private Vector2 mousePos;
        private int brushSize = 0;
        private int pixelType = 2;
        
        private bool isPaintHeldDown = false;
        private bool isEraseHeldDown = false;

        public PixelElement[] pixels =
        [
            PixelFactory.CreateAir(),
            PixelFactory.CreateLiquid(),
            PixelFactory.CreateScaffolding(),
            PixelFactory.CreateSolid()
        ];
        
        public override void _Ready()
        {
            previewSprite = new Sprite2D();
            previewImage = Image.CreateEmpty((int)ParentWorld.WorldSize.X, (int)ParentWorld.WorldSize.Y, false, Image.Format.Rgba8);
            previewImage.Fill(Colors.Transparent);

            Position = new Vector2(ParentWorld.WindowSize.X/(ParentWorld.PixelSize.X*2), ParentWorld.WindowSize.Y/(ParentWorld.PixelSize.Y*2));
            
            AddChild(previewSprite);
            previewSprite.Texture = ImageTexture.CreateFromImage(previewImage);
        }
        
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (isPaintHeldDown)
            {
                EmitSignal(SignalName.PaintRequested, mousePos, pixelType, brushSize);
            }
            
            if (isEraseHeldDown)
            {
                EmitSignal(SignalName.EraseRequested, mousePos, brushSize);
            }

            DrawPreview();
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                int number = (int)keyEvent.Keycode - (int)Key.Key0;
                if (number > 0 && number < pixels.Length + 1)
                {
                    pixelType = number - 1;
                }
            }

            if (@event is InputEventMouseMotion eventMouseMotion)
            {
                mousePos = GetGlobalMousePosition();
            }
            
            if (!Godot.Input.IsMouseButtonPressed(MouseButton.Left))
            {
                isPaintHeldDown = false;
            }
        
            if (Godot.Input.IsMouseButtonPressed(MouseButton.Left))
            {
                isPaintHeldDown = true;
                EmitSignal(SignalName.PaintRequested, mousePos, pixelType, brushSize);
            }
            
            if (!Godot.Input.IsMouseButtonPressed(MouseButton.Right))
            {
                isEraseHeldDown = false;
            }
        
            if (Godot.Input.IsMouseButtonPressed(MouseButton.Right))
            {
                isEraseHeldDown = true;
            }

            if (Godot.Input.IsMouseButtonPressed(MouseButton.WheelUp))
            {
                brushSize++;
            }
            if (Godot.Input.IsMouseButtonPressed(MouseButton.WheelDown))
            {
                brushSize--;
            }
        }

        private Color GetPixelTypeColor()
        {
            if (pixelType > pixels.Length - 1)
            {
                pixelType = pixels.Length - 1;  
            }
                
            if (pixelType < 0)
            {
                pixelType = 0;
            }

            return pixels[pixelType].Color;
        }

        private void DrawPreview()
        {
            Vector2I pos = ParentWorld.CamToWorld(mousePos);
            int size = brushSize;
            previewImage.Fill(Colors.Transparent);
            
            // Generate all positions within the circle
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    // Check if the position is within the circle using distance formula
                    float distance = Mathf.Sqrt(x * x + y * y);
                    Vector2I p = new Vector2I(pos.X + x, pos.Y +  y);

                    if (!ParentWorld.IsInBound(p))
                    {
                        continue;
                    }
                    
                    if (distance >= size - 0.5f && distance <= size + 0.5f)
                    {
                        previewImage.SetPixel(p.X, p.Y, GetPixelTypeColor());
                    }
                }
            }
            previewSprite.Texture = ImageTexture.CreateFromImage(previewImage);
        }
    }
}