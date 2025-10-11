using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public class TriangleSplitter
{
    /// <summary>
    /// Splits a triangle into three smaller triangles by connecting a new vertex to the original triangle's vertices.
    /// </summary>
    /// <param name="triangle">The original triangle to split.</param>
    /// <param name="newVertex">The new vertex that will be connected to the original triangle's vertices.</param>
    /// <returns>A list containing the three smaller faces resulting from the split.</returns>
    public void SplitTriangle(Face triangle, Vertex newVertex)
    {
        // Split the triangle into three smaller triangles
        var originalEdges = triangle.GetEdges().ToArray();
        var originalVertices = triangle.GetVertices().ToList();

        // Extract triangle vertices.
        var (A, B, C) =
            (originalEdges[0].Origin,
            originalEdges[1].Origin,
            originalEdges[2].Origin);



        // Create new half-edge pairs (twins) connecting the new vertex to each triangle vertex.
        var (dToA, aToD) = HalfEdge.CreateHalfEdgePair(newVertex, A);
        var (dToB, bToD) = HalfEdge.CreateHalfEdgePair(newVertex, B);
        var (dToC, cToD) = HalfEdge.CreateHalfEdgePair(newVertex, C);

        // Create the 3 new faces.
        var face1 = new Face(dToA, originalEdges[0], bToD);
        var face2 = new Face(dToB, originalEdges[1], cToD);
        var face3 = new Face(dToC, originalEdges[2], aToD);

        // Return the faces as a list
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


        var face1 = new Face(newToB,adjacentEdge1,xToNew);

        var face2= new Face(newToX, adjacentEdge2, aToNew);


        var face3 = new Face(newToA, twinAdjacentEdge1, yToNew);

        var face4 = new Face(newToY, twinAdjacentEdge2, bToNew);


    }
}
