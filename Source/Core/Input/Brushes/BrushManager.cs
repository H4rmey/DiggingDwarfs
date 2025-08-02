using Godot;
using System.Collections.Generic;
using System.Linq;
using SharpDiggingDwarfs.Core.Physics.Elements;
using SharpDiggingDwarfs.Core.Physics.Factory;
using System;

namespace SharpDiggingDwarfs.Core.Input.Brushes
{
    /// <summary>
    /// Represents a pixel type that can be selected in the brush system
    /// </summary>
    public struct PixelTypeInfo
    {
        public string Name { get; init; }
        public Func<PixelElement> Factory { get; init; }
        public PixelType Type { get; init; }
        
        public PixelTypeInfo(string name, Func<PixelElement> factory, PixelType type)
        {
            Name = name;
            Factory = factory;
            Type = type;
        }
    }

    /// <summary>
    /// Manages brush selection, size, and pixel type selection with centralized pixel type registry
    /// </summary>
    public class BrushManager
    {
        public const int MIN_BRUSH_SIZE = 0;
        public const int MAX_BRUSH_SIZE = 32;
        
        private List<IBrush> availableBrushes;
        private int currentBrushIndex;
        private int currentSize;
        private int currentPixelTypeIndex;
        private PixelTypeInfo[] availablePixelTypes;
        
        public IBrush CurrentBrush => availableBrushes[currentBrushIndex];
        public int CurrentSize => currentSize;
        public PixelElement CurrentPixelType => availablePixelTypes[currentPixelTypeIndex].Factory();
        public string CurrentBrushName => CurrentBrush.Name;
        public string CurrentPixelTypeName => availablePixelTypes[currentPixelTypeIndex].Name;
        public int AvailablePixelTypesCount => availablePixelTypes.Length;
        public int AvailableBrushesCount => availableBrushes.Count;
        
        /// <summary>
        /// Gets all available pixel type information
        /// </summary>
        public PixelTypeInfo[] GetAvailablePixelTypes() => availablePixelTypes;
        
        /// <summary>
        /// Gets pixel type info by index
        /// </summary>
        public PixelTypeInfo GetPixelTypeInfo(int index)
        {
            if (index >= 0 && index < availablePixelTypes.Length)
                return availablePixelTypes[index];
            return availablePixelTypes[0]; // Default to first type
        }
        
        public BrushManager()
        {
            // Initialize available brushes
            availableBrushes = new List<IBrush>
            {
                new CircleBrush(),
                new SquareBrush()
            };
            
            // Initialize available pixel types with centralized registry
            availablePixelTypes = new PixelTypeInfo[]
            {
                new PixelTypeInfo("Air", PixelFactory.CreateAir, PixelType.Empty),
                new PixelTypeInfo("Solid", PixelFactory.CreateSolid, PixelType.Solid),
                new PixelTypeInfo("Liquid", PixelFactory.CreateLiquid, PixelType.Liquid),
                new PixelTypeInfo("Structure", PixelFactory.CreateStructure, PixelType.Structure)
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
        /// Sets the pixel type directly by index
        /// </summary>
        public void SetPixelTypeIndex(int index)
        {
            if (index >= 0 && index < availablePixelTypes.Length)
            {
                currentPixelTypeIndex = index;
            }
        }
        
        /// <summary>
        /// Creates a pixel of the specified type index
        /// </summary>
        public PixelElement CreatePixelByIndex(int index)
        {
            if (index >= 0 && index < availablePixelTypes.Length)
            {
                return availablePixelTypes[index].Factory();
            }
            return availablePixelTypes[0].Factory(); // Default to first type
        }
        
        /// <summary>
        /// Sets the brush type directly by index
        /// </summary>
        public void SetBrushIndex(int index)
        {
            if (index >= 0 && index < availableBrushes.Count)
            {
                currentBrushIndex = index;
            }
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
        /// Gets debug information about current brush settings
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Brush: {CurrentBrushName} | Size: {CurrentSize} | Type: {CurrentPixelTypeName}";
        }
    }
}