using System;
using System.Collections.Generic;
using System.Linq;

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
    public static List<Face> SplitTriangle(Face triangle, Vertex newVertex)
    {
        // Assuming _triangleSplitter is an instance of TriangleSplitter
        return _triangleSplitter.SplitTriangle(triangle, newVertex);
    }

        public static List<Face> SplitTriangle_VertexOnEdge(HalfEdge edge, Vertex newVertex)
    {
        // Assuming _triangleSplitter is an instance of TriangleSplitter
        return _triangleSplitter.SplitTriangle_VertexOnEdge(edge, newVertex);
    }

    /// <summary>
    /// Prepares vertices and edge constraints for triangulation, including adding supertriangle and sorting.
    /// </summary>
    public static void getSuperTriangle(
        ref List<Vertex> vertices,
        out Face superTriangle
    )
    {
        // Call the PrepareData function from TriangulationPreprocessor
        _supertriangleGenerator
            .getSuperTriangle(ref vertices, out superTriangle);
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
