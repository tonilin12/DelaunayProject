using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; // Assuming you use System.Numerics.Vector2

public class SuperTriangleGenerator
{
    public void getSuperTriangle(
        ref List<Vertex> vertices,
        out Face superTriangle
    )
    {


       float factor = 10.0f;




        // 2. Compute initial bounding box (before expansion)
        float minX = vertices.Min(v => v.Position.X);
        float minY = vertices.Min(v => v.Position.Y);
        float maxX = vertices.Max(v => v.Position.X);
        float maxY = vertices.Max(v => v.Position.Y);

        // 3. Expand bounding box with margin (currently zero, you can change if needed)
        float marginX = (maxX - minX) * factor;
        float marginY = (maxY - minY) * factor;
        minX -= marginX;
        minY -= marginY;
        maxX += marginX;
        maxY += marginY;

     

        // 5. Calculate supertriangle size based on expanded bounding box
        float dx = maxX - minX;
        float dy = maxY - minY;
        float a = Math.Max(1000.0f, factor * Math.Max(dx, dy));

        // 6. Create supertriangle vertices
        float midX = (minX + maxX) / 2.0f;
        float midY = (minY + maxY) / 2.0f;
        Vector2 center = new Vector2(midX, midY);

        Vertex vA 
            = new Vertex(new Vector2(center.X - a, center.Y - a / (float)Math.Sqrt(3)));
        Vertex vB 
            = new Vertex(new Vector2(center.X + a, center.Y - a / (float)Math.Sqrt(3)));
        Vertex vC 
            = new Vertex(new Vector2(center.X, center.Y + 2 * a / (float)Math.Sqrt(3)));

    

        // 10. Create supertriangle face
        superTriangle = new Face(vA, vB, vC);
    }
}
