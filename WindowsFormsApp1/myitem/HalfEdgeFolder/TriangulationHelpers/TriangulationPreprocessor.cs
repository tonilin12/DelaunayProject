using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; // Assuming you use System.Numerics.Vector2

public class TriangulationPreprocessor
{
    public void PrepareData(
        ref List<Vertex> vertices,
        ref List<(Vertex Origin, Vertex Dest)> edgeTuples,  // Using value tuples
        out Face superTriangle
    )
    {
        const float factor = 10.0f;
        HashSet<Vertex> vertexSet = new HashSet<Vertex>(vertices);

        // 1. Collect initial vertices and edge vertices
        foreach (var edge in edgeTuples)
        {
            vertexSet.Add(edge.Origin);
            vertexSet.Add(edge.Dest);
        }

        // 2. Compute initial bounding box (before expansion)
        float minX = vertexSet.Min(v => v.Position.X);
        float minY = vertexSet.Min(v => v.Position.Y);
        float maxX = vertexSet.Max(v => v.Position.X);
        float maxY = vertexSet.Max(v => v.Position.Y);

        // 3. Expand bounding box with margin (currently zero, you can change if needed)
        float marginX = (maxX - minX) * 0.0f;
        float marginY = (maxY - minY) * 0.0f;
        minX -= marginX;
        minY -= marginY;
        maxX += marginX;
        maxY += marginY;

        // Optional: Uncomment to add boundary edges and vertices if needed
        /*
        var boundaryEdges = new List<(Vector2, Vector2)>
        {
            (new Vector2(minX, minY), new Vector2(maxX, minY)),
            (new Vector2(maxX, minY), new Vector2(maxX, maxY)),
            (new Vector2(maxX, maxY), new Vector2(minX, maxY)),
            (new Vector2(minX, maxY), new Vector2(minX, minY))
        };

        foreach (var edge in boundaryEdges)
        {
            var from = new Vertex(edge.Item1);
            var to = new Vertex(edge.Item2);
            edgeTuples.Add((from, to));
            vertexSet.Add(from);
            vertexSet.Add(to);
        }
        */

        // 5. Calculate supertriangle size based on expanded bounding box
        float dx = maxX - minX;
        float dy = maxY - minY;
        float a = Math.Max(1000.0f, factor * Math.Max(dx, dy));

        // 6. Create supertriangle vertices
        float midX = (minX + maxX) / 2.0f;
        float midY = (minY + maxY) / 2.0f;
        Vector2 center = new Vector2(midX, midY);

        Vertex vA = new Vertex(new Vector2(center.X - a, center.Y - a / (float)Math.Sqrt(3)));
        Vertex vB = new Vertex(new Vector2(center.X + a, center.Y - a / (float)Math.Sqrt(3)));
        Vertex vC = new Vertex(new Vector2(center.X, center.Y + 2 * a / (float)Math.Sqrt(3)));

        // 8. Convert to list and sort AFTER all additions
        vertices = vertexSet.ToList();
        vertices.Sort((v1, v2) =>
        {
            int cmp = v1.Position.Y.CompareTo(v2.Position.Y);
            return cmp != 0 ? cmp : v1.Position.X.CompareTo(v2.Position.X);
        });

        // Optional: Uncomment to sort edges if needed
        /*
        edgeTuples.Sort((e1, e2) =>
        {
            int cmp = e1.Origin.Position.Y.CompareTo(e2.Origin.Position.Y);
            if (cmp == 0) cmp = e1.Origin.Position.X.CompareTo(e2.Origin.Position.X);

            if (cmp == 0)
            {
                cmp = e1.Dest.Position.Y.CompareTo(e2.Dest.Position.Y);
                if (cmp == 0) cmp = e1.Dest.Position.X.CompareTo(e2.Dest.Position.X);
            }

            return cmp;
        });
        */

        // 10. Create supertriangle face
        superTriangle = new Face(vA, vB, vC);
    }
}
