using System.Collections.Generic;
using Godot;
using Godot.Collections;

public enum PixelState
{
    EMPTY,
    SOLID,
    FALLING
}

public class PixelData 
{
    public Color Color { get; set; }
    public PixelState State { get; set; }
    public bool IsFalling { get; set; }
    public bool HasBeenChecked { get; set; }
    public float ShockFallChance { get; set; }
    public float FallChance { get; set; }

    public PixelData(Color color, PixelState state, bool isFalling = false, float shockFallChance = 0.0f, float fallChance = 0.0f)
    {
        Color = color;
        State = state;
        IsFalling = isFalling;
        ShockFallChance = shockFallChance;
        FallChance = fallChance;
        HasBeenChecked = false;
    }
    
    public PixelData(PixelData other)
    {
        Color = other.Color;
        State = other.State;
        IsFalling = other.IsFalling;
        ShockFallChance = other.ShockFallChance;
        FallChance = 0.25f;
        HasBeenChecked = other.HasBeenChecked;
    }

    public PixelData()
    {
        Color = Colors.Black;
        State = PixelState.EMPTY;
        IsFalling = false;
        ShockFallChance = 0.25f;
        FallChance = 0.25f;
        HasBeenChecked = false;
    }
    
    public void SetPixelData(Color color, PixelState state, bool isFalling = false, float fallChance = 0.0f)
    {
        Color = color;
        State = state;
        IsFalling = isFalling;
        ShockFallChance = fallChance;
        HasBeenChecked = false;
    }
}

public partial class PixelJunk : Node2D
{
    [ExportGroup("parameters")]
    [Export]
    private Vector2I chunkSize = new Vector2I(320, 180);
    [Export]
    private Vector2  randomShockwaveIntervalRange = new Vector2(20.0f, 80.0f);

    private Image image;
    private Vector2I mousePos;
    private Vector2 viewPortSize;
    
    private Sprite2D sprite;
    private StaticBody2D staticBody;
    
    // pdg = PixelDataGrid
    private PixelData[,] pixelDataGrid;
    private List<Vector2I> pixelsToAdd;
    private List<Vector2I> pixelsToRemove;
    
    private float counter  = 0.00f;
    private float counter2 = 0.00f;
    private float randomShockwaveInterval = 2.0f;
    

    public override void _Ready()
    {
        Position   = new Vector2(chunkSize.X / 2, chunkSize.Y / 2);
        sprite     = new Sprite2D();
        staticBody = new StaticBody2D();
        
        AddChild(sprite);
        sprite.AddChild(staticBody);

        image = new Image();
        image = Image.Create(chunkSize.X, chunkSize.Y, false, Image.Format.Rgba8);

        pixelsToAdd    = new List<Vector2I>();
        pixelsToRemove = new List<Vector2I>();
        
        pixelDataGrid = InitPixedDataGrid();
        GenerateMap();
        RefreshChunk();
        
        viewPortSize = GetViewport().GetVisibleRect().Size;
        float width  = (int)viewPortSize.X / image.GetWidth();
        float height = (int)viewPortSize.Y / image.GetHeight();
        Scale    = new Vector2(width, height);
        Position = new Vector2(viewPortSize.X / 2, viewPortSize.Y / 2);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.Right)
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        SetPixelData(mousePos.X + x, mousePos.Y + y, new Color(0,0,0,0), PixelState.EMPTY, false);
                        CheckSurroundingPixels(mousePos.X + x, mousePos.Y + y);
                    }
                }
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                for (int x = -3; x <= 3; x++)
                {
                    for (int y = -3; y <= 3; y++)
                    {
                        SetPixelData(mousePos.X + x, mousePos.Y + y, Colors.Blue, PixelState.SOLID, true);
                        CheckSurroundingPixels(mousePos.X + x, mousePos.Y + y);
                    }
                }
            }
            else if (eventMouseButton.ButtonIndex == MouseButton.Middle)
            {
                SetPixelData(mousePos.X, mousePos.Y, Colors.Pink, PixelState.SOLID, true);
            }
            RefreshChunk();
        }
        else if (@event is InputEventMouseMotion eventMouseMotion)
        {
            // TODO: /4 is dirty
            Vector2I rawMouse = (Vector2I)eventMouseMotion.Position;
            mousePos = new Vector2I(rawMouse.X/4, rawMouse.Y / 4);
        }
    }

    public override void _Process(double delta)
    {
        counter += (float)delta;
        counter2 += (float)delta;

        if (counter2 > randomShockwaveInterval)
        {
            for (int x = 0; x < image.GetWidth() - 1; x++)
            {
                for (int y = image.GetHeight() - 1; y > 0; y--)
                {
                    RandomPixelFallCheck(x, y);
                }
            }

            counter2 = 0.0f;
            randomShockwaveInterval = (float)GD.RandRange(randomShockwaveIntervalRange.X, randomShockwaveIntervalRange.Y);
            GD.Print("RUMBLING!!!!   " + randomShockwaveInterval.ToString("0.0"));
        }
        
        if (counter > 0.000001f)
        {
            RefreshChunk();
            counter = 0.0f;
        }
    }

    private PixelData[,] InitPixedDataGrid()
    {
        PixelData[,] dataGrid = new PixelData[chunkSize.X, chunkSize.Y];
        
        for (int y = 0; y < chunkSize.Y; y++)
        {
            for (int x = 0; x < chunkSize.X; x++)
            {
                dataGrid[x, y] = new PixelData(Colors.SaddleBrown, PixelState.SOLID, false, 0.1f, 0.75f); 
            }
        }

        return dataGrid;
    }

    private void RefreshChunk()
    {
        GenerateNextPixelPositions();
        PaintImageWithPixelData(); 
        //AddCollisionShapes();
    }

    private void PaintImageWithPixelData()
    {
        //for (int y = 0; y < image.GetHeight(); y++)
        //{
        //    for (int x = 0; x < image.GetWidth(); x++)
        //    {
        //        if (pixelDataGrid[x, y] != null)
        //        {
        //            image.SetPixel(x,y,pixelDataGrid[x,y].Color);
        //            //pixelDataGrid[x,y].HasBeenChecked = false;
        //        }
        //    }
        //}
        for (int i = 0; i < pixelsToAdd.Count; i++)
        {
            Vector2I add = pixelsToAdd[i];
            Vector2I remove = pixelsToRemove[i];
            PixelData removeData = pixelDataGrid[remove.X, remove.Y];
            SetPixelData(add.X, add.Y, removeData.Color, removeData.State, true);
            SetPixelData(remove.X, remove.Y, new Color(0,0,0,0), PixelState.EMPTY, false);
        }
        sprite.Texture = ImageTexture.CreateFromImage(image);
        pixelsToAdd.Clear();
        pixelsToRemove.Clear();
    }

    private PixelData[,] CopyPixelDataArray(PixelData[,] source)
    {
        PixelData[,] destination = new PixelData[source.GetLength(0), source.GetLength(1)];
        for (int x =0; x < source.GetLength(0); x++)
        {
            for (int y =0; y < source.GetLength(1); y++)
            {
                destination[x, y] = new PixelData(source[x, y]);
            }
        }
        return destination;
    }

    private void GenerateNextPixelPositions()
    {
        for (int y = 0; y < chunkSize.Y; y++)
        {
            for (int x = 0; x < chunkSize.X; x++)
            {
                SetFuturePixelPosition(x, y);
            }
        }
    }

    private void SetFuturePixelPosition(int x, int y)
    {
        //bool c = pixelDataGrid[x, y].IsFalling;
        //if (c)
        //{
        //    if (pixelDataGrid[x, y + 1].State != PixelState.SOLID)
        //    {
        //        pixelsToAdd.Add(new Vector2I(x, y+1));
        //        pixelsToRemove.Add(new Vector2I(x, y));
        //    }
        //}


        if (!pixelDataGrid[x, y].IsFalling)
        {
            return;
        }

        Vector2I[] pixels = new Vector2I[]
        {
            new Vector2I(x, y + 1),
            new Vector2I(x + 1, y + 1),
            new Vector2I(x - 1, y + 1)
        };

        if ((float)GD.RandRange(0.0, 1.0) > 0.5)
        {
            Vector2I t_pix = pixels[1];
            pixels[1] = pixels[2];
            pixels[2] = t_pix;
        }

        foreach (Vector2I p in pixels)
        {
            if (p.X < 0 || p.X >= pixelDataGrid.GetLength(0) - 1 ||
                p.Y < 0 || p.Y >= pixelDataGrid.GetLength(1) - 1)
            {
                continue;
            }

            if (pixelDataGrid[p.X, p.Y].State == PixelState.SOLID)
            {
                continue;
            }

            if (pixelsToAdd.Contains(p))
            {
                continue;
            }
            
            // if next pixel is not solid, let the pixel fall
            pixelsToAdd.Add(p);
            pixelsToRemove.Add(new Vector2I(x, y));
            CheckSurroundingPixels(x, y);
            return; 
        }
    }
    
    private void CheckSurroundingPixels(int x, int y)
    {
        if (pixelDataGrid[x, y] == null)
        {
            return;
        }
        
        Vector2I[] pixels = new Vector2I[]
        {
            new Vector2I(x + 1, y + 1),
            new Vector2I(x - 1, y + 1),
            new Vector2I(x, y + 1),
            new Vector2I(x + 1, y - 1),
            new Vector2I(x - 1, y - 1),
            new Vector2I(x, y - 1),
            new Vector2I(x + 1, y),
            new Vector2I(x - 1, y)
        };

        foreach (Vector2I p in pixels)
        {
            if (p.X < 0 || p.X >= pixelDataGrid.GetLength(0) - 1 ||
                p.Y < 0 || p.Y >= pixelDataGrid.GetLength(1) - 1)
            {
                continue;
            }

            if (pixelDataGrid[p.X, p.Y].HasBeenChecked)
            {
                continue; 
            }
            pixelDataGrid[p.X, p.Y].HasBeenChecked = true;

            if (pixelDataGrid[p.X, p.Y].State != PixelState.SOLID)
            {
                continue;
            }
            
            float randomNumber = (float)GD.RandRange(0.0, 1.0);
            if (randomNumber > pixelDataGrid[p.X, p.Y].ShockFallChance)
            {
                continue;
            }

            pixelDataGrid[p.X, p.Y].IsFalling = true;
        }
    }
    
    private void RandomPixelFallCheck(int x, int y)
    {
        if (pixelDataGrid[x,y].State != PixelState.SOLID)
        {
            //if no continue
            return;
        }
        
        Vector2I[] pixels = new Vector2I[]
        {
            new Vector2I(x + 1, y + 1),
            new Vector2I(x - 1, y + 1),
            new Vector2I(x    , y + 1),
            new Vector2I(x + 1, y - 1),
            new Vector2I(x - 1, y - 1),
            new Vector2I(x    , y - 1),
            new Vector2I(x + 1, y),
            new Vector2I(x - 1, y)
        };

        int solidCounter = 0;
        foreach (Vector2I p in pixels)
        {
            // is the pixel in the map?
            if (p.X < 0 || p.X >= pixelDataGrid.GetLength(0)-1 ||
                p.Y < 0 || p.Y >= pixelDataGrid.GetLength(1)-1)
            {
                // if no count as solid and continue
                solidCounter++;
                continue;
            }
           
            // is the pixel solid
            if (pixelDataGrid[p.X, p.Y].State != PixelState.SOLID)
            {
                //if no continue
                continue;
            }
            
            solidCounter++;
        }

        float fallChance = (solidCounter / (pixels.Length-1.95f));
        switch(solidCounter) 
        {
            case 0:
                pixelDataGrid[x, y].IsFalling = true;
                return;
            case 1:
                fallChance = 0.93f;
                break;
            case 2:
                fallChance = 0.94f;
                break;
            case 3:
                fallChance = 0.95f;
                break;
            case 4:
                fallChance = 0.96f;
                break;
            case 5:
                fallChance = 0.97f;
                break;
            case 6:
                fallChance = 0.98f;
                break;
            case 7:
                return;
            case 8:
                return;
            default:
                return;
        }
        
        float randomNumber = (float)GD.RandRange(0.0, 1.0);
        if (randomNumber > fallChance)
        {
            pixelDataGrid[x, y].IsFalling = true;
        }
    }
    

    private void GenerateMap()
    {
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                float noiseValue = x / 2 + 100;
                SetPixelData(x, y, new Color(0,0,0,0), PixelState.EMPTY, false);
                if (y > noiseValue)
                {
                    SetPixelData(x, y, Colors.Orange, PixelState.SOLID, false);
                }
                else if (y < image.GetHeight() / 2 - 50)
                {
                    SetPixelData(x,y,Colors.Orange, PixelState.SOLID, false);
                }
                else if (y > image.GetHeight() - 10)
                {
                    SetPixelData(x,y,Colors.Orange, PixelState.SOLID, false);
                }
            }
        }
    }

    private void SetPixelData(int x, int y, Color color, PixelState state, bool isFalling)
    {
        if (x < 0 || y < 0 || x >= chunkSize.X-1 || y >= chunkSize.Y-1)
        {
            return;
        }

        pixelDataGrid[x, y].Color = color;
        pixelDataGrid[x, y].State = state;
        pixelDataGrid[x, y].IsFalling = isFalling;
        image.SetPixel(x, y, pixelDataGrid[x, y].Color);
        
        if (pixelDataGrid[x, y].State == PixelState.SOLID)
        {
            pixelDataGrid[x, y].Color = Colors.Green;
        }
        else
        {
            pixelDataGrid[x, y].Color = Colors.Purple;
        }
        
        if (isFalling)
        {
            pixelDataGrid[x, y].Color = Colors.Blue;
        }
    }
    
    private void AddCollisionShapes()
    {
        foreach (Node child in staticBody.GetChildren())
        {
            child.QueueFree();
        }

        Bitmap bitmap = new Bitmap();
        bitmap.CreateFromImageAlpha(image);
        Array<Vector2[]> polygons = bitmap.OpaqueToPolygons(new Rect2I(Vector2I.Zero, image.GetSize()), 1.0f);
        foreach (Vector2[] poly in polygons)
        {
            CollisionPolygon2D collisionShape = new CollisionPolygon2D();
            staticBody.AddChild(collisionShape);
            collisionShape.BuildMode = CollisionPolygon2D.BuildModeEnum.Segments;
            collisionShape.Polygon = poly;
            collisionShape.Position = new Vector2(-chunkSize.X/2, -chunkSize.Y/2);
        }
    }
}
