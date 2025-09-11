using System;
using System.Linq;
using System.Collections.Generic;

public class TriangleSplitter
{
    /// <summary>
    /// Splits a triangle into three smaller triangles by connecting a new vertex to the original triangle's vertices.
    /// </summary>
    /// <param name="triangle">The original triangle to split.</param>
    /// <param name="newVertex">The new vertex that will be connected to the original triangle's vertices.</param>
    /// <returns>A list containing the three smaller faces resulting from the split.</returns>
    public List<Face> SplitTriangle(Face triangle, Vertex newVertex)
    {
        // Split the triangle into three smaller triangles
        var originalEdges = triangle.GetEdges().ToArray();
        var originalVertices = triangle.GetVertices().ToList();

        // Extract triangle vertices.
        var (A, B, C) =
            (originalEdges[0].Origin,
            originalEdges[1].Origin,
            originalEdges[2].Origin);


        var newfaces = new List<Face>();
        var len=originalVertices.Count;


        for (int i = 0; i < len; i++)
        {
            int next = (i + 1) % len;
            var face0 = new Face(newVertex, originalVertices[i], originalVertices[next]);
            newfaces.Add(face0);
        }

        // Create new half-edge pairs (twins) connecting the new vertex to each triangle vertex.
        var (dToA, aToD) = HalfEdge.CreateHalfEdgePair(newVertex, A);
        var (dToB, bToD) = HalfEdge.CreateHalfEdgePair(newVertex, B);
        var (dToC, cToD) = HalfEdge.CreateHalfEdgePair(newVertex, C);

        // Create the 3 new faces.
        var face1 = new Face(dToA, originalEdges[0], bToD);
        var face2 = new Face(dToB, originalEdges[1], cToD);
        var face3 = new Face(dToC, originalEdges[2], aToD);

        // Return the faces as a list
        return new List<Face> { face1, face2, face3 };
    }

    public List<Face>
    SplitTriangleWithEdge(HalfEdge edge, Vertex newVertex)
    {
        // Assume the shared edge runs from A to B.
        // In the first triangle (face of 'edge'):
        // - edge goes from A to B.
        // - adjacentEdge1 = edge.Next goes from B to X.
        // - adjacentEdge2 = edge.Prev goes from X to A.
        //
        // In the second triangle (face of 'edge.Twin'):
        // - edge.Twin goes from B to A.
        // - twinAdjacentEdge1 = edge.Twin.Next goes from A to Y.
        // - twinAdjacentEdge2 = edge.Twin.Prev goes from Y to B.

        // Retrieve neighboring edges from both adjacent faces.
        var adjacentEdge1 = edge.Next;      // B -> X in triangle1
        var adjacentEdge2 = edge.Prev;        // X -> A in triangle1
        var twinAdjacentEdge1 = edge.Twin.Next; // A -> Y in triangle2
        var twinAdjacentEdge2 = edge.Twin.Prev;   // Y -> B in triangle2

        // Split the shared edge (A <-> B) by inserting newVertex:
        // Create a pair to represent the new segment from A to newVertex.
        var (aToNew, newToA) = HalfEdge.CreateHalfEdgePair(edge.Origin, newVertex);
        // Create a pair to represent the new segment from newVertex to B.
        var (bToNew, newToB) = HalfEdge.CreateHalfEdgePair(edge.Twin.Origin, newVertex);

        // Create new half-edge pair connecting newVertex with the opposite vertex in triangle1 (vertex X).
        var (xToNew, newToX) = HalfEdge.CreateHalfEdgePair(adjacentEdge2.Origin, newVertex);
        // Create new half-edge pair connecting newVertex with the opposite vertex in triangle2 (vertex Y).
        var (yToNew, newToY) = HalfEdge.CreateHalfEdgePair(twinAdjacentEdge2.Origin, newVertex);

        // Build the 4 new faces.

        // For the first original triangle (A, B, X):
        // It is split into:
        //   Face1: A, newVertex, X  
        //     - Edges: A -> newVertex (aToNew), newVertex -> X (newToX), X -> A (existing adjacentEdge2)
        var face1 = new Face(aToNew, newToX, adjacentEdge2);

        //   Face2: newVertex, B, X  
        //     - Edges: newVertex -> B (newToB), B -> X (existing adjacentEdge1), X -> newVertex (xToNew)
        var face2 = new Face(newToB, adjacentEdge1, xToNew);

        // For the second original triangle (B, A, Y):
        // It is split into:
        //   Face3: B, newVertex, Y  
        //     - Edges: B -> newVertex (bToNew), newVertex -> Y (newToY), Y -> B (existing twinAdjacentEdge2)
        var face3 = new Face(bToNew, newToY, twinAdjacentEdge2);

        //   Face4: newVertex, A, Y  
        //     - Edges: newVertex -> A (newToA), A -> Y (existing twinAdjacentEdge1), Y -> newVertex (yToNew)
        var face4 = new Face(newToA, twinAdjacentEdge1, yToNew);

        // Return the list of newly created faces.
        return new List<Face> { face1, face2, face3, face4 };
    }
}
