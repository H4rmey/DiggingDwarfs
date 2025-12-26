using Godot;

namespace SharpDiggingDwarfs.Source.Core.Rendering;

public partial class DebugImage : Node2D
{
    public Vector2I Size = new Vector2I(32, 18);
    private Image Image;
    private Sprite2D Sprite;
    private ImageTexture Texture;

    # region DEBUG
    public void init(Vector2I size)
    {
        Size = size;
        Sprite = new Sprite2D();
        Image  = new Image();
        
        Image = Image.CreateEmpty(Size.X, Size.Y, false, Image.Format.Rgba8);
        Image.Fill(Colors.Transparent);
        Texture = ImageTexture.CreateFromImage(Image);
        Sprite.Texture = Texture;
        AddChild(Sprite);
    }

    public void DrawBorder(Color color)
    {
        for (int x = 0; x < Image.GetSize().X; x++)
        {
            for (int y = 0; y < Image.GetSize().Y; y++)
            {
                if (x == 0 || y == 0 || x == Size.X - 1 || y == Size.Y - 1)
                {
                    Image.SetPixel(x, y, color);
                }
            }
        }
        Texture.Update(Image);
    }
    
    public void ColorPixel(Vector2I pos, Color color)
    {
        Image.SetPixelv(pos, color);
        Texture.Update(Image);
    }

    public void ClearImage()
    {
        Image.Fill(Colors.Transparent);
        Texture.Update(Image);
    }
    # endregion
}
