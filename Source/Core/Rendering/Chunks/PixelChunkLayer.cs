using Godot;
using SharpDiggingDwarfs.Core.Physics.Factory;

namespace SharpDiggingDwarfs.Source.Core.Rendering.Chunks;

public partial class PixelChunkLayer : Node2D
{
    private Vector2I _size = new Vector2I(32, 18);
    public Image image { get; private set;  }
    private Sprite2D _sprite;
    private ImageTexture _texture;

    public void init(Vector2I size)
    {
        _size = size;
        _sprite = new Sprite2D();
        image  = new Image();
        
        image = Image.CreateEmpty(_size.X, _size.Y, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        _texture = ImageTexture.CreateFromImage(image);
        _sprite.Texture = _texture;
        AddChild(_sprite);
    }

    public void InitEmpty()
    {
        for (int x = 0; x < _size.X; x++)
        {
            for (int y = 0; y < _size.Y; y++)
            {
                image.SetPixel(x, y, Colors.Transparent);
            }
        }
    }
    
    public void ColorPixel(Vector2I pos, Color color) { image.SetPixelv(pos, color); }

    public void Update() { _texture.Update(image); }
    
    public bool IsInBound(Vector2I pos) { return pos.X >= 0 && pos.X < _size.X && pos.Y >= 0 && pos.Y < _size.Y; }
}
