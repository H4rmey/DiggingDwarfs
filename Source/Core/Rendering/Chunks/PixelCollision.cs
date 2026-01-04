using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class PixelCollision : Node2D
{
    [Export] public float AlphaThreshold = 0.1f;
    [Export] public Color OutlineColor = new Color(1, 0, 0, 0.25f);
    [Export] public float LineWidth = 0.25f;

    private StaticBody2D body = new StaticBody2D();
    private Array<Vector2[]> _polygons = new();

    public override void _Ready()
    {
        AddChild(body);
    }

    // Renamed from "Update" to avoid conflict with Node2D.Update()
    public void Update(Image image)
    {
        // Clear old collisions
        foreach (Node child in body.GetChildren())
            child.QueueFree();
        
        foreach (Node child in GetChildren())
        {
            if (child is Line2D line)
                line.QueueFree();
        }
        
        Bitmap bitMap = new Bitmap();
        bitMap.CreateFromImageAlpha(image, AlphaThreshold);

        // Convert bitmap → polygons
        _polygons = bitMap.OpaqueToPolygons(new Rect2I(Vector2I.Zero, image.GetSize()));

        foreach (Vector2[] poly in _polygons)
        {
            CollisionPolygon2D collision = new CollisionPolygon2D();
            collision.Polygon = poly;
            body.AddChild(collision);
            
            // Create Line2D for visible outline
            Line2D line = new Line2D();
            line.Points = poly; // set points
            line.Closed = true; // close the polygon
            line.Width = LineWidth;
            line.DefaultColor = OutlineColor;
            AddChild(line);
        }

    }

    public override void _Draw()
    {
        foreach (var poly in _polygons)
        {
            if (poly.Length < 2) continue;

            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 start = poly[i];
                Vector2 end = poly[(i + 1) % poly.Length];
                DrawLine(start, end, OutlineColor, LineWidth);
            }
        }
    }
}