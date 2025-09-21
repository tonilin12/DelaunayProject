using System;
using System.Collections.Generic;
using System.Linq;

public class FlipHelper
{
    /// <summary>
    /// Flips the given half-edge, which is assumed to be the shared edge between two adjacent triangular faces.
    /// After the flip, the two faces will have their opposite vertices swapped while preserving consistency.
    /// </summary>
    /// <param name="edge">The half-edge to flip (passed by reference).</param>
    public void FlipEdge(ref HalfEdge edge)
    {
        // Ensure the edge and its twin are valid.
        if (edge == null || edge.Twin == null)
        {
            throw new InvalidOperationException("The provided half-edge must have a valid twin.");
        }

        // Retrieve the two adjacent faces.
        Face face1 = edge.Face;
        Face face2 = edge.Twin.Face;
        if (face1 == null || face2 == null)
        {
            throw new InvalidOperationException("The edge must belong to two faces.");
        }

        // Retrieve adjacent half-edges.
        HalfEdge e0 = edge;          // Shared edge (to be flipped)
        HalfEdge f0 = edge.Twin;     // Twin of the shared edge
        HalfEdge e1 = e0.Next;       // Next edge in face1
        HalfEdge e2 = e1.Next;       // Third edge in face1
        HalfEdge f1 = f0.Next;       // Next edge in face2
        HalfEdge f2 = f1.Next;       // Third edge in face2

        // Get the original vertices.
        // Naming convention:
        // - Face1 has vertices A, B, D (in order: A = e0.Origin, B = e1.Origin, D = e2.Origin)
        // - Face2 has vertex C (with C = f2.Origin)
        Vertex A = e0.Origin;
        Vertex B = e1.Origin;
        Vertex D = e2.Origin;
        Vertex C = f2.Origin;

        // Flip the shared edge:
        // After the flip, the new shared edge should connect vertex D (from face1) and vertex C (from face2).
        // So, we reassign the origins:
        e0.Origin = D;  // e0 now goes from D -> (its destination determined by e0.Next)
        f0.Origin = C;  // f0 now goes from C -> (its destination determined by f0.Next)

        // Reconnect half-edges to form the new face cycles.
        // For face1: the new cycle is: e0 -> f2 -> e1.
        e0.Next = f2;
        f2.Next = e1;
        e1.Next = e0;

        // For face2: the new cycle is: f0 -> e2 -> f1.
        f0.Next = e2;
        e2.Next = f1;
        f1.Next = f0;

  

        // Update face references so that each half-edge points to the correct face.
        e0.Face = face1;
        f2.Face = face1;
        e1.Face = face1;
        face1.Edge = e0;

        f0.Face = face2;
        e2.Face = face2;
        f1.Face = face2;
        face2.Edge = f0;

        // Update the twin relationship for the flipped edge.
        e0.Twin = f0;
        f0.Twin = e0;

        // Update the outgoing half-edge pointer for each vertex in the affected faces.
        foreach (var e in face1.GetEdges())
            e.Origin.OutgoingHalfEdge = e;

        foreach (var e in face2.GetEdges())
            e.Origin.OutgoingHalfEdge = e;


        // Ensure the reference points to the flipped edge.
        edge = e0;
    }
}
