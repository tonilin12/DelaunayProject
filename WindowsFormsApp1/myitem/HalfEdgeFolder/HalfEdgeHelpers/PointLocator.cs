using System;
using System.Collections.Generic;
using System.Numerics;
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

    /// <summary>
    /// Computes point location relative to a face.
    /// </summary>
    private static PointLocationResult GetPointLocation(HalfEdge startEdge, Vertex point)
    {
        bool allPositive = true;
        bool anyOnEdge = false;
        HalfEdge nextHalfEdge = null;
        HalfEdge exactMatchEdge = null;
        HalfEdge firstOnEdge = null;

        foreach (var e in startEdge.Face.GetEdges())
        {
            int orientation = TriangleOrientation(e.Origin, e.Dest, point);
            bool onEdge = orientation == 0 && IsOnSegment(e.Origin, e.Dest, point);

            // Track if point is on any edge
            if (onEdge)
            {
                anyOnEdge = true;
                if (e.Origin.PositionsEqual(point))
                    exactMatchEdge = e;      // prefer exact match
                else if (firstOnEdge == null)
                    firstOnEdge = e;        // fallback
            }

            if (orientation <= 0) allPositive = false;

            if (orientation < 0 && nextHalfEdge == null)
                nextHalfEdge = e.Twin;        // first negative edge
        }

        return new PointLocationResult
        {
            IsInside = allPositive,
            IsOnEdge = anyOnEdge,
            DestinationEdge = exactMatchEdge ?? firstOnEdge,
            NextHalfEdge = (!allPositive && !anyOnEdge) ? nextHalfEdge : null
        };
    }

    /// <summary>
    /// Locate the face or edge containing a point in a half-edge mesh.
    /// Returns the destination edge, a flag for being on an edge, and traversed edges.
    /// </summary>
    private static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
    LocatePointInMesh(HalfEdge startEdge, Vertex point)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        var traversed = new List<HalfEdge>();
        var current = startEdge;

        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            if (current == null)
                throw new InvalidOperationException("Encountered null half-edge during traversal.");

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
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
    LocatePointInMesh(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));
        if (startFace.Edge == null)
            throw new ArgumentException("Face must have a valid edge.", nameof(startFace));

        return LocatePointInMesh(startFace.Edge, point);
    }

}
