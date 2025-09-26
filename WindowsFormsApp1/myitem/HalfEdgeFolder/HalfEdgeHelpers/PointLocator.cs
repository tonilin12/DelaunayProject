using System;
using System.Collections.Generic;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;
using static WindowsFormsApp1.myitem.GeometryFolder.GeometryUtils;

public static class PointLocator
{
    private const int MAX_ITERATIONS = 1000;

    /// <summary>
    /// Checks if point P lies on segment AB, including endpoints.
    /// </summary>
    private static bool IsOnSegment(Vertex a, Vertex b, Vertex p)
    {
        Vector2 ap = p.Position - a.Position;
        Vector2 ab = b.Position - a.Position;

        float dot = Vector2.Dot(ap, ab);
        if (dot < 0) return false;         // before start
        float lenSq = ab.LengthSquared();
        if (dot > lenSq) return false;     // after end
        return true;                        // on segment, including endpoints
    }

    private class PointLocationResult
    {
        public bool IsInside { get; set; }
        public bool IsOnEdge { get; set; }
        public HalfEdge DestinationEdge { get; set; }
        public HalfEdge NextHalfEdge { get; set; }
    }

    /// <summary>
    /// Computes point location relative to a face.
    /// </summary>
    private static PointLocationResult 
    GetPointLocation(HalfEdge startEdge, Vertex point)
    {
        bool anyOnEdge = false;
        bool allPositive = true;

        HalfEdge exactMatchEdge = null;
        HalfEdge firstOnEdge = null;
        HalfEdge nextHalfEdge = null;

        float minPositiveOrientation = float.MaxValue;
        HalfEdge minPositiveEdge = null;

        foreach (var e in startEdge.Face.GetEdges())
        {
            // Compute signed area (TriangleOrientation returns float)
            float orientation =GetSignedArea(e.Origin, e.Dest, point);

            // Check if point is on this edge
            bool onEdge = Math.Abs(orientation) < GeometryUtils.GetEpsilon && IsOnSegment(e.Origin, e.Dest, point);

            if (onEdge)
            {
                anyOnEdge = true;

                if (e.Origin.PositionsEqual(point))
                    exactMatchEdge = e; // prefer exact match
                else if (firstOnEdge == null)
                    firstOnEdge = e;
            }

            // Track if all orientations are positive (inside)
            if (orientation <= 0) allPositive = false;

            // Track first negative orientation for walking outside
            if (orientation < 0 && nextHalfEdge == null)
                nextHalfEdge = e.Twin;

            // Track smallest positive orientation (closest to leaving)
            if (orientation > 0 && orientation < minPositiveOrientation)
            {
                minPositiveOrientation = orientation;
                minPositiveEdge = e;
            }
        }

        return new PointLocationResult
        {
            IsInside = allPositive,
            IsOnEdge = anyOnEdge,
            DestinationEdge = exactMatchEdge ?? firstOnEdge ?? minPositiveEdge,
            NextHalfEdge = (!allPositive && !anyOnEdge) ? nextHalfEdge : null
        };
    }



    /// <summary>
    /// Internal traversal function, optional recording of edges.
    /// </summary>
    private static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
    LocatePointInMesh(HalfEdge startEdge, Vertex point, bool recordTraversal)
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

            if (result.IsInside)
                return (current, false, traversed);

            if (result.IsOnEdge)
                return (result.DestinationEdge, true, traversed);

            if (result.NextHalfEdge == null)
                throw new InvalidOperationException(
                    "Point outside face but no adjacent twin found (non-manifold or boundary)."
                );

            current = result.NextHalfEdge;
        }

        throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }

    /// <summary>
    /// Public entry point: locate a point starting from a face.
    /// Optional parameter to record traversed edges.
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