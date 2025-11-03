using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class TriangleSplitter
{
    /// <summary>
    /// Splits a triangle into three smaller triangles by connecting a new vertex to the original triangle's vertices.
    /// </summary>
    /// <param name="triangle">The original triangle to split.</param>
    /// <param name="newVertex">The new vertex that will be connected to the original triangle's vertices.</param>
    public void SplitTriangle(Face triangle, Vertex newVertex)
    {
        // Use the edge iterator directly
        var it = triangle.GetEdgeIterator();

        // Extract the three edges and vertices directly
        if (!it.MoveNext())
            throw new InvalidOperationException("Face does not have exactly 3 edges.");
        var edge1 = it.Current!;
        var A = edge1.Origin;

        if (!it.MoveNext())
            throw new InvalidOperationException("Face does not have exactly 3 edges.");
        var edge2 = it.Current!;
        var B = edge2.Origin;

        if (!it.MoveNext())
            throw new InvalidOperationException("Face does not have exactly 3 edges.");
        var edge3 = it.Current!;
        var C = edge3.Origin;

        // Verify triangle has exactly 3 edges
        if (it.MoveNext())
            throw new InvalidOperationException("Face does not have exactly 3 edges.");

        // Create new half-edge pairs connecting the new vertex to each triangle vertex
        var (dToA, aToD) = HalfEdge.CreateHalfEdgePair(newVertex, A);
        var (dToB, bToD) = HalfEdge.CreateHalfEdgePair(newVertex, B);
        var (dToC, cToD) = HalfEdge.CreateHalfEdgePair(newVertex, C);

        // Create the 3 new faces
        new Face(dToA, edge1, bToD);
        new Face(dToB, edge2, cToD);
        new Face(dToC, edge3, aToD);

        // Now face1, face2, face3 are the new triangles
    }




    public void
    SplitTriangle_VertexOnEdge(HalfEdge edge, Vertex newVertex)
    {


       // Assume the shared edge runs from A to B.
       // In the first triangle (face of 'edge'):
       // - edge goes from A to B.
       // - adjacentEdge1 = edge.Next goes from B to X.
       // - adjacentEdge2 = edge.Next.Next goes from X to A.
       //
       // In the second triangle (face of 'edge.Twin'):
       // - edge.Twin goes from B to A.
       // - twinAdjacentEdge1 = edge.Twin.Next goes from A to Y.
       // - twinAdjacentEdge2 = edge.Twin.Next.Next goes from Y to B.

       // Retrieve neighboring edges from both adjacent faces.
       var adjacentEdge1 = edge.Next;      // B -> X in triangle1
        var adjacentEdge2 = edge?.Next?.Next;        // X -> A in triangle1
        var twinAdjacentEdge1 = edge?.Twin?.Next; // A -> Y in triangle2
        var twinAdjacentEdge2 = edge?.Twin?.Next?.Next;   // Y -> B in triangle2


        // Create a pair to represent the new segment from newVertex to B.
        var (newToB, bToNew) = HalfEdge.CreateHalfEdgePair(newVertex, adjacentEdge1.Origin);


        // Create new half-edge pair connecting newVertex with the opposite vertex in triangle1 (vertex X).
        var (newToX, xToNew) = HalfEdge.CreateHalfEdgePair(newVertex, adjacentEdge2.Origin);



        // Split the shared edge (A <-> B) by inserting newVertex:
        // Create a pair to represent the new segment from A to newVertex.
        var (newToA, aToNew) = HalfEdge.CreateHalfEdgePair(newVertex, twinAdjacentEdge1.Origin);


        // Create new half-edge pair connecting newVertex with the opposite vertex in triangle2 (vertex Y).
        var (newToY, yToNew) = HalfEdge.CreateHalfEdgePair(newVertex, twinAdjacentEdge2.Origin);


        new Face(newToB, adjacentEdge1, xToNew);
        new Face(newToX, adjacentEdge2, aToNew);
        new Face(newToA, twinAdjacentEdge1, yToNew);
        new Face(newToY, twinAdjacentEdge2, bToNew);

    }
}
