using System;
using System.Linq;
using System.Numerics;

public class SuperTriangleGenerator
{
    public Face GetSuperTriangle(Vertex[] vertices)
    {
        if (vertices == null || vertices.Length == 0)
            throw new ArgumentException("Vertex array cannot be null or empty", nameof(vertices));

        float factor = 2.0f;

        // Compute initial bounding box
        float minX = vertices.Min(v => v.Position.X);
        float minY = vertices.Min(v => v.Position.Y);
        float maxX = vertices.Max(v => v.Position.X);
        float maxY = vertices.Max(v => v.Position.Y);

        // Expand bounding box with factor
        float marginX = (maxX - minX) * factor;
        float marginY = (maxY - minY) * factor;
        minX -= marginX;
        minY -= marginY;
        maxX += marginX;
        maxY += marginY;

        // Determine supertriangle size
        float dx = maxX - minX;
        float dy = maxY - minY;
        float a = Math.Max(1000.0f, factor * Math.Max(dx, dy));

        // Compute center of bounding box
        float midX = (minX + maxX) / 2.0f;
        float midY = (minY + maxY) / 2.0f;
        Vector2 center = new Vector2(midX, midY);

        // Create supertriangle vertices
        Vertex vA = new Vertex(center.X - a, center.Y - a / (float)Math.Sqrt(3));
        Vertex vB = new Vertex(center.X + a, center.Y - a / (float)Math.Sqrt(3));
        Vertex vC = new Vertex(center.X, center.Y + 2 * a / (float)Math.Sqrt(3));

        // Return supertriangle
        return new Face(vA, vB, vC);
    }
}
