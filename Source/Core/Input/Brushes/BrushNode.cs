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
    /// </summary>
    public partial class BrushNode : Node2D
    {
        [Signal]
        public delegate void BrushChangedEventHandler(string brushName, int size, string pixelType);
        
        [Signal]
        public delegate void PaintRequestedEventHandler(Vector2I position, int pixelTypeIndex);
        
        [Signal]
        public delegate void EraseRequestedEventHandler(Vector2I position);
        
        [ExportGroup("Brush Settings")]
        [Export] public bool EnableInput { get; set; } = true;
        [Export] public bool ShowPreview { get; set; } = true;
        [Export] public int InitialSize { get; set; } = 0;
        [Export] public int InitialBrushIndex { get; set; } = 0;
        [Export] public int InitialPixelTypeIndex { get; set; } = 1;
        
        private BrushManager brushManager;
        private Sprite2D previewSprite;
        private Image previewImage;
        private Vector2I currentMousePosition;
        private Vector2I chunkSize = new Vector2I(128, 72); // Default chunk size
        
        public BrushManager Manager => brushManager;
        public Vector2I MousePosition => currentMousePosition;
        
        public override void _Ready()
        {
            // Initialize brush manager
            brushManager = new BrushManager();
            brushManager.SetBrushSize(InitialSize);
            
            // Set initial brush and pixel type if valid
            for (int i = 0; i < InitialBrushIndex; i++)
                brushManager.NextBrush();
            
            for (int i = 0; i < InitialPixelTypeIndex; i++)
                brushManager.NextPixelType();
            
            // Create preview sprite
            if (ShowPreview)
            {
                SetupPreview();
            }
            
            // Emit initial state
            EmitBrushChanged();
        }
        
        private void SetupPreview()
        {
            previewSprite = new Sprite2D();
            previewImage = Image.CreateEmpty(chunkSize.X, chunkSize.Y, false, Image.Format.Rgba8);
            previewImage.Fill(Colors.Transparent);
            
            AddChild(previewSprite);
            previewSprite.Texture = ImageTexture.CreateFromImage(previewImage);
        }
        
        public override void _Input(InputEvent @event)
        {
            if (!EnableInput) return;
            
            if (@event is InputEventMouseMotion eventMouseMotion)
            {
                UpdateMousePosition(eventMouseMotion.Position);
            }
            
            HandleBrushInput(@event);
            HandlePaintInput(@event);
        }
        
        private void HandleBrushInput(InputEvent @event)
        {
            bool brushChanged = false;
            
            if (GodotInput.IsKeyPressed(Key.Shift))
            {
                // Shift + Mouse Wheel = Change brush size
                if (GodotInput.IsMouseButtonPressed(MouseButton.WheelDown))
                {
                    brushManager.DecreaseBrushSize();
                    brushChanged = true;
                }
                else if (GodotInput.IsMouseButtonPressed(MouseButton.WheelUp))
                {
                    brushManager.IncreaseBrushSize();
                    brushChanged = true;
                }
            }
            else if (GodotInput.IsKeyPressed(Key.Ctrl))
            {
                // Ctrl + Mouse Wheel = Change brush shape
                if (GodotInput.IsMouseButtonPressed(MouseButton.WheelDown))
                {
                    brushManager.PreviousBrush();
                    brushChanged = true;
                }
                else if (GodotInput.IsMouseButtonPressed(MouseButton.WheelUp))
                {
                    brushManager.NextBrush();
                    brushChanged = true;
                }
            }
            else
            {
                // Mouse Wheel = Change pixel type
                if (GodotInput.IsMouseButtonPressed(MouseButton.WheelDown))
                {
                    brushManager.PreviousPixelType();
                    brushChanged = true;
                }
                else if (GodotInput.IsMouseButtonPressed(MouseButton.WheelUp))
                {
                    brushManager.NextPixelType();
                    brushChanged = true;
                }
            }
            
            if (brushChanged)
            {
                UpdatePreview();
                EmitBrushChanged();
            }
        }
        
        private void HandlePaintInput(InputEvent @event)
        {
            if (GodotInput.IsMouseButtonPressed(MouseButton.Left))
            {
                // Paint with current pixel type - emit for each position
                var paintPositions = brushManager.GetPaintPositions(currentMousePosition);
                foreach (var pos in paintPositions)
                {
                    EmitSignal(SignalName.PaintRequested, pos, GetCurrentPixelTypeIndex());
                }
            }
            else if (GodotInput.IsMouseButtonPressed(MouseButton.Right))
            {
                // Erase (paint with air) - emit for each position
                var erasePositions = brushManager.GetPaintPositions(currentMousePosition);
                foreach (var pos in erasePositions)
                {
                    EmitSignal(SignalName.EraseRequested, pos);
                }
            }
        }
        
        private void UpdateMousePosition(Vector2 screenPosition)
        {
            // Convert screen position to chunk coordinates
            // This assumes the chunk is scaled to fit the viewport
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            Vector2 scale = new Vector2(viewportSize.X / chunkSize.X, viewportSize.Y / chunkSize.Y);
            
            currentMousePosition = new Vector2I(
                (int)(screenPosition.X / scale.X),
                (int)(screenPosition.Y / scale.Y)
            );
            
            UpdatePreview();
        }
        
        private void UpdatePreview()
        {
            if (!ShowPreview || previewSprite == null) return;
            
            // Clear preview
            previewImage.Fill(Colors.Transparent);
            
            // Get preview positions
            var previewPositions = brushManager.GetPreviewPositions(currentMousePosition);
            
            // Draw preview
            foreach (var pos in previewPositions)
            {
                if (IsValidPosition(pos))
                {
                    Color previewColor = brushManager.CurrentPixelType.BaseColor;
                    previewColor.A = 0.7f; // Semi-transparent
                    previewImage.SetPixel(pos.X, pos.Y, previewColor);
                }
            }
            
            previewSprite.Texture = ImageTexture.CreateFromImage(previewImage);
        }
        
        private bool IsValidPosition(Vector2I position)
        {
            return position.X >= 0 && position.X < chunkSize.X && 
                   position.Y >= 0 && position.Y < chunkSize.Y;
        }
        
        private void EmitBrushChanged()
        {
            EmitSignal(SignalName.BrushChanged, 
                brushManager.CurrentBrushName, 
                brushManager.CurrentSize, 
                brushManager.CurrentPixelTypeName);
        }
        
        // Public API methods for external control
        
        /// <summary>
        /// Sets the chunk size for coordinate calculations and preview bounds
        /// </summary>
        public void SetChunkSize(Vector2I size)
        {
            chunkSize = size;
            if (ShowPreview && previewImage != null)
            {
                previewImage = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
                previewImage.Fill(Colors.Transparent);
                UpdatePreview();
            }
        }
        
        /// <summary>
        /// Manually trigger a paint action at the specified position
        /// </summary>
        public void PaintAt(Vector2I position)
        {
            var paintPositions = brushManager.GetPaintPositions(position);
            foreach (var pos in paintPositions)
            {
                EmitSignal(SignalName.PaintRequested, pos, GetCurrentPixelTypeIndex());
            }
        }
        
        /// <summary>
        /// Manually trigger an erase action at the specified position
        /// </summary>
        public void EraseAt(Vector2I position)
        {
            var erasePositions = brushManager.GetPaintPositions(position);
            foreach (var pos in erasePositions)
            {
                EmitSignal(SignalName.EraseRequested, pos);
            }
        }
        
        /// <summary>
        /// Get the current pixel type index for signal emission
        /// </summary>
        private int GetCurrentPixelTypeIndex()
        {
            var currentPixel = brushManager.CurrentPixelType;
            return currentPixel.Type switch
            {
                PixelType.Empty => 0,
                PixelType.Solid => 1,
                PixelType.Liquid => 2,
                _ => 0
            };
        }
        
        /// <summary>
        /// Get a pixel of the specified type index
        /// </summary>
        public PixelElement GetPixelByIndex(int index)
        {
            return index switch
            {
                0 => PixelFactory.CreateAir(),
                1 => PixelFactory.CreateSolid(),
                2 => PixelFactory.CreateLiquid(),
                _ => PixelFactory.CreateAir()
            };
        }
        
        /// <summary>
        /// Get current brush information
        /// </summary>
        public string GetBrushInfo()
        {
            return brushManager.GetDebugInfo();
        }
        
        /// <summary>
        /// Enable or disable input handling
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            EnableInput = enabled;
        }
        
        /// <summary>
        /// Enable or disable preview display
        /// </summary>
        public void SetPreviewEnabled(bool enabled)
        {
            ShowPreview = enabled;
            if (previewSprite != null)
            {
                previewSprite.Visible = enabled;
            }
        }
    }
}