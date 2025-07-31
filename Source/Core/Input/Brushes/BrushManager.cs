using Godot;
using System.Collections.Generic;
using System.Linq;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;

namespace SharpDiggingDwarfs.Core.Input.Brushes
{
    /// <summary>
    /// Manages brush selection, size, and pixel type selection
    /// </summary>
    public class BrushManager
    {
        public const int MIN_BRUSH_SIZE = 0;
        public const int MAX_BRUSH_SIZE = 32;
        
        private List<IBrush> availableBrushes;
        private int currentBrushIndex;
        private int currentSize;
        private int currentPixelTypeIndex;
        private PixelElement[] availablePixelTypes;
        
        public IBrush CurrentBrush => availableBrushes[currentBrushIndex];
        public int CurrentSize => currentSize;
        public PixelElement CurrentPixelType => availablePixelTypes[currentPixelTypeIndex];
        public string CurrentBrushName => CurrentBrush.Name;
        public string CurrentPixelTypeName => GetPixelTypeName(CurrentPixelType);
        
        public BrushManager()
        {
            // Initialize available brushes
            availableBrushes = new List<IBrush>
            {
                new CircleBrush(),
                new SquareBrush()
            };
            
            // Initialize available pixel types
            availablePixelTypes = new PixelElement[]
            {
                PixelFactory.CreateAir(),
                PixelFactory.CreateSolid(),
                PixelFactory.CreateLiquid(),
                PixelFactory.CreateStructure()
            };
            
            // Set default values
            currentBrushIndex = 0; // Start with circle brush
            currentSize = MIN_BRUSH_SIZE;
            currentPixelTypeIndex = 1; // Start with solid pixels
        }
        
        /// <summary>
        /// Changes to the next brush shape
        /// </summary>
        public void NextBrush()
        {
            currentBrushIndex = (currentBrushIndex + 1) % availableBrushes.Count;
        }
        
        /// <summary>
        /// Changes to the previous brush shape
        /// </summary>
        public void PreviousBrush()
        {
            currentBrushIndex = (currentBrushIndex - 1 + availableBrushes.Count) % availableBrushes.Count;
        }
        
        /// <summary>
        /// Increases brush size within limits
        /// </summary>
        public void IncreaseBrushSize()
        {
            currentSize = Mathf.Clamp(currentSize + 1, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
        }
        
        /// <summary>
        /// Decreases brush size within limits
        /// </summary>
        public void DecreaseBrushSize()
        {
            currentSize = Mathf.Clamp(currentSize - 1, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
        }
        
        /// <summary>
        /// Sets brush size directly within limits
        /// </summary>
        public void SetBrushSize(int size)
        {
            currentSize = Mathf.Clamp(size, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
        }
        
        /// <summary>
        /// Changes to the next pixel type
        /// </summary>
        public void NextPixelType()
        {
            currentPixelTypeIndex = (currentPixelTypeIndex + 1) % availablePixelTypes.Length;
        }
        
        /// <summary>
        /// Changes to the previous pixel type
        /// </summary>
        public void PreviousPixelType()
        {
            currentPixelTypeIndex = (currentPixelTypeIndex - 1 + availablePixelTypes.Length) % availablePixelTypes.Length;
        }
        
        /// <summary>
        /// Gets the positions that would be affected by painting at the given position
        /// </summary>
        public List<Vector2I> GetPaintPositions(Vector2I centerPosition)
        {
            return CurrentBrush.GetAffectedPositions(centerPosition, currentSize);
        }
        
        /// <summary>
        /// Gets the positions for brush preview/highlight
        /// </summary>
        public List<Vector2I> GetPreviewPositions(Vector2I centerPosition)
        {
            return CurrentBrush.GetPreviewPositions(centerPosition, currentSize);
        }
        
        /// <summary>
        /// Creates a clone of the current pixel type for painting
        /// </summary>
        public PixelElement CreateCurrentPixel()
        {
            return CurrentPixelType.Clone();
        }
        
        /// <summary>
        /// Gets a human-readable name for a pixel type
        /// </summary>
        private string GetPixelTypeName(PixelElement pixel)
        {
            return pixel.Type switch
            {
                PixelType.Empty => "Air",
                PixelType.Solid => "Solid",
                PixelType.Liquid => "Liquid",
                PixelType.Structure => "Structure",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Gets debug information about current brush settings
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Brush: {CurrentBrushName} | Size: {CurrentSize} | Type: {CurrentPixelTypeName}";
        }
    }
}