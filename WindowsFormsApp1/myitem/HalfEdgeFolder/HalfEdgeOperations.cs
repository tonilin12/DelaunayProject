using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class HalfEdgeOperations
{
    public static HalfEdge findSharedEdgeFor2Face(Face face1, Face face2)
    {
        if (face1 == null || face2 == null)
            throw new ArgumentNullException("Face objects cannot be null.");

        var edges1 = face1.GetEdges();
        var edges2 = face2.GetEdges();

        if (edges1 == null || edges2 == null)
            throw new InvalidOperationException("Edges collection cannot be null.");

        // Find edge in face1 whose reversed counterpart exists in face2
        var sharedEdge = edges1.FirstOrDefault(edge1 =>
            edges2.Any(edge2 =>
                edge1.Dest == edge2.Origin && edge1.Origin == edge2.Dest
            )
        );

        return sharedEdge; // May be null if no shared edge
    }

    public static List<Vertex> GetQuadrilateral(HalfEdge sharedEdge)
    {
        if (sharedEdge == null || sharedEdge.Twin == null)
            return null;

        var twinEdge = sharedEdge.Twin;

        // The quadrilateral is formed by vertices of shared edge and their next edges in both faces
        return new List<Vertex>
        {
            sharedEdge.Dest,
            sharedEdge.Next.Dest,
            twinEdge.Dest,
            twinEdge.Next.Dest
        };
    }

    public static List<HalfEdge> findIntersectingEdges(HashSet<Face> triangles, Vertex a, Vertex b)
    {
        if (triangles == null || a == null || b == null)
            throw new ArgumentNullException("Input arguments cannot be null.");

        var allEdges = triangles.SelectMany(t => t.GetEdges()).ToList();

        var intersectingEdges = new HashSet<HalfEdge>();

        foreach (var edge in allEdges)
        {
            Vector2 s1 = edge.Origin.Position;
            Vector2 e1 = edge.Dest.Position;
            Vector2 s2 = a.Position;
            Vector2 e2 = b.Position;

            if (GeometryUtils.AreSegmentsCrossing(s1, e1, s2, e2))
            {
                // Prevent adding reverse edge if it already exists
                bool reverseExists = intersectingEdges.Any(e =>
                    e.Origin == edge.Dest && e.Dest == edge.Origin);

                if (!reverseExists)
                {
                    intersectingEdges.Add(edge);
                }
            }
        }

        return intersectingEdges.Count > 0 ? intersectingEdges.ToList() : new List<HalfEdge>();
    }
}
