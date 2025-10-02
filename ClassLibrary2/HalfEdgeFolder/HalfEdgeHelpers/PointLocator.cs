using System;
using System.Collections.Generic;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;
using static WindowsFormsApp1.myitem.GeometryFolder.GeometryUtils;

public static class PointLocator
{
    private const int MAX_ITERATIONS = 1000;

    private class PointLocationResult
    {
        public bool IsInside { get; set; }
        public bool IsOnEdge { get; set; }
        public HalfEdge DestinationEdge { get; set; }
        public HalfEdge NextHalfEdge { get; set; }
    }

    private static bool IsOnSegment(Vertex a, Vertex b, Vertex p)
    {
        Vector2 ap = p.Position - a.Position;
        Vector2 ab = b.Position - a.Position;
        float dot = Vector2.Dot(ap, ab);
        if (dot < 0) return false;
        float lenSq = ab.LengthSquared();
        if (dot > lenSq) return false;
        return true;
    }

    private static PointLocationResult GetPointLocation(HalfEdge startEdge, Vertex point)
    {
        bool anyOnEdge = false;
        bool allPositive = true;
        HalfEdge exactVertexEdge = null;
        HalfEdge firstEdgeOnSegment = null;
        HalfEdge nextHalfEdge = null;

        Func<Vertex, bool> IsExactVertex = v => v != null && v.PositionsEqual(point);

        Func<HalfEdge, bool> IsPointOnEdge = edge =>
        {
            if (edge == null || edge.Origin == null || edge.Dest == null) return false;
            float orientation = GetSignedArea(edge.Origin, edge.Dest, point);
            return Math.Abs(orientation) < GeometryUtils.GetEpsilon && IsOnSegment(edge.Origin, edge.Dest, point);
        };

        foreach (var edge in startEdge.Face.GetEdges())
        {
            if (edge == null || edge.Origin == null || edge.Dest == null) continue;

            if (IsExactVertex(edge.Origin))
                exactVertexEdge = edge;

            bool onEdge = IsPointOnEdge(edge);
            if (onEdge)
            {
                anyOnEdge = true;
                if (firstEdgeOnSegment == null && exactVertexEdge == null)
                    firstEdgeOnSegment = edge;
            }

            float orientation = GetSignedArea(edge.Origin, edge.Dest, point);
            if (orientation <= 0) allPositive = false;

            if (orientation < 0 && nextHalfEdge == null)
                nextHalfEdge = edge.Twin;
        }

        return new PointLocationResult
        {
            IsInside = !anyOnEdge && allPositive,
            IsOnEdge = anyOnEdge,
            DestinationEdge = exactVertexEdge ?? firstEdgeOnSegment,
            NextHalfEdge = (!allPositive && !anyOnEdge) ? nextHalfEdge : null
        };
    }

    /// <summary>
    /// Core traversal function starting from a half-edge
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
    LocatePointInMesh(HalfEdge startEdge, Vertex point, bool recordTraversal = false)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        List<HalfEdge> traversed = recordTraversal ? new List<HalfEdge>() : null;
        HalfEdge current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            if (current == null)
                throw new InvalidOperationException("Encountered null half-edge during traversal.");

            if (recordTraversal)
                traversed.Add(current);

            var result = GetPointLocation(current, point);

            if (result.DestinationEdge != null && result.DestinationEdge.Origin.PositionsEqual(point))
                return (result.DestinationEdge, true, traversed); // existing vertex

            if (result.IsOnEdge)
                return (result.DestinationEdge, true, traversed); // point on edge

            if (result.IsInside)
                return (current, false, traversed); // inside triangle

            if (result.NextHalfEdge == null)
                throw new InvalidOperationException("Point outside face but no adjacent twin found.");

            current = result.NextHalfEdge;
        }

        throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }

    /// <summary>
    /// Overload: start search from a face instead of a half-edge
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
    LocatePointInMesh(Face startFace, Vertex point, bool recordTraversal = false)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));
        if (startFace.Edge == null)
            throw new ArgumentException("Face must have a valid edge.", nameof(startFace));

        return LocatePointInMesh(startFace.Edge, point, recordTraversal);
    }
}
