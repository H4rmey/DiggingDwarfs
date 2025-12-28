using Godot;
using System;
using System.Collections.Generic;

public partial class PixelCollision : Node2D
{
    private readonly List<StaticBody2D> _bodies = new();

    /// <summary>
    /// Regenerate collisions from an image
    /// </summary>
    /// <param name="image">Image to generate collisions from (transparent pixels ignored)</param>
    /// <param name="alphaThreshold">Alpha threshold for opaque pixels</param>
    public void Update(Image image, float alphaThreshold = 0.1f)
    {
        // Remove previous bodies
        foreach (var body in _bodies)
            body.QueueFree();
        _bodies.Clear();

        // Create a BitMap from the image alpha
        Bitmap bitmap = new Bitmap();
        bitmap.CreateFromImageAlpha(image, alphaThreshold);

        // Generate polygons with marching squares
        Godot.Collections.Array<Vector2[]> polygons =
            bitmap.OpaqueToPolygons(new Rect2I(Vector2I.Zero, image.GetSize()));

        foreach (Vector2[] polygon in polygons)
        {
            // Skip invalid or tiny polygons
            if (polygon.Length < 3 || GetPolygonArea(polygon) < 0.1f)
                continue;

            CreateCollisionBody(polygon);
        }
    }

    private void CreateCollisionBody(Vector2[] polygon)
    {
        StaticBody2D body = new StaticBody2D();
        AddChild(body);
        _bodies.Add(body);

        CollisionPolygon2D collision = new CollisionPolygon2D
        {
            Polygon = polygon
        };

        body.AddChild(collision);
    }

    /// <summary>
    /// Compute the area of a polygon using the shoelace formula
    /// </summary>
    private float GetPolygonArea(Vector2[] polygon)
    {
        float area = 0f;
        int n = polygon.Length;

        for (int i = 0; i < n; i++)
        {
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % n];
            area += (current.X * next.Y) - (next.X * current.Y);
        }

        return Math.Abs(area * 0.5f);
    }
}
