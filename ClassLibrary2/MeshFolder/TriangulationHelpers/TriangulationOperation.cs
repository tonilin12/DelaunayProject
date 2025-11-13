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
    public static Face GetSuperTriangle(Vertex[] vertices)
    {
       return _supertriangleGenerator.GetSuperTriangle(vertices);
    }





    /// <summary>
    /// Flips the common edge between two adjacent triangles to maintain Delaunay condition.
    /// </summary>
    /// <param name="face1">First adjacent face.</param>
    /// <param name="face2">Second adjacent face.</param>
    /// <returns>Nothing, the method modifies the faces directly.</returns>
    public static void FlipEdge( HalfEdge edge)
    {
        _flipHelper.FlipEdge( edge);
    }

}
