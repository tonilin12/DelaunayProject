using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class TriangulationOperation
{
    private static readonly TriangleSplitter _triangleSplitter = new TriangleSplitter();
    private static readonly SuperTriangleGenerator _supertriangleGenerator
        = new SuperTriangleGenerator();
    private static readonly FlipHelper _flipHelper = new FlipHelper();



    /// <summary>
    /// Splits a triangle face into three smaller triangles by connecting a new vertex to each vertex of the original triangle.
    /// </summary>
    /// <param name="triangle">The face (triangle) to be split.</param>
    /// <param name="newVertex">The vertex used to split the triangle.</param>
    /// <returns>A list of 3 new triangle faces formed after the split.</returns>
    public static void SplitTriangle(Face triangle, Vertex newVertex)
    {
        // Assuming _triangleSplitter is an instance of TriangleSplitter
       _triangleSplitter.SplitTriangle(triangle, newVertex);
    }

    public static void SplitTriangle_VertexOnEdge(HalfEdge edge, Vertex newVertex)
    {
        // Assuming _triangleSplitter is an instance of TriangleSplitter
        _triangleSplitter.SplitTriangle_VertexOnEdge(edge, newVertex);
    }

    /// <summary>
    /// Prepares vertices and edge constraints for triangulation, including adding supertriangle and sorting.
    /// </summary>
    public static void getSuperTriangle(
        ref Vertex[] vertices,
        out Face superTriangle
    )
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

        // Create supertriangle face
        superTriangle = new Face(vA, vB, vC);
    }



    /// <summary>
    /// Flips the common edge between two adjacent triangles to maintain Delaunay condition.
    /// </summary>
    /// <param name="face1">First adjacent face.</param>
    /// <param name="face2">Second adjacent face.</param>
    /// <returns>Nothing, the method modifies the faces directly.</returns>
    public static void FlipEdge(ref HalfEdge edge)
    {
        _flipHelper.FlipEdge(ref edge);
    }

}
