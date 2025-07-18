using System;
using System.Collections.Generic;
using System.Linq;

public static class TriangulationOperation
{
    private static readonly TriangleSplitter _triangleSplitter = new TriangleSplitter();
    private static readonly TriangulationPreprocessor _triangulationPreprocessor = new TriangulationPreprocessor();
    private static readonly FlipHelper _flipHelper = new FlipHelper();

    private static readonly BaseTriangulationClass 
    _baseTriangulationClass = new BaseTriangulationClass();



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

        public static List<Face> SplitTriangleWithEdge(HalfEdge edge, Vertex newVertex)
    {
        // Assuming _triangleSplitter is an instance of TriangleSplitter
        return _triangleSplitter.SplitTriangleWithEdge(edge, newVertex);
    }

    /// <summary>
    /// Prepares vertices and edge constraints for triangulation, including adding supertriangle and sorting.
    /// </summary>
    /// <param name="vertices">List of vertices for triangulation.</param>
    /// <param name="edgeTuples">List of edge constraints as vertex pairs.</param>
    /// <param name="superTriangle">Output parameter for the enclosing supertriangle face.</param>
    public static void PrepareData(
        ref List<Vertex> vertices,
        ref List<(Vertex, Vertex)> edgeTuples,  // Use value tuples instead of Tuple
        out Face superTriangle
    )
    {
        // Call the PrepareData function from TriangulationPreprocessor
        _triangulationPreprocessor
            .PrepareData(ref vertices, ref edgeTuples, out superTriangle);
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

    public static bool InCircle(Face triangle,Vertex p)
    {
        var vertices = triangle.GetVertices().Select(x=>x.Position).ToList();
        var (a, b, c) = (vertices[0], vertices[1], vertices[2]);
        return GeometryUtils.InCircle(a,b,c,p.Position);
    }

    public static HashSet<Face> GetTriangulation(List<Vertex> points, Face supertriangle)
    {
        return _baseTriangulationClass.getTriangulation(points, supertriangle);
    }


}
