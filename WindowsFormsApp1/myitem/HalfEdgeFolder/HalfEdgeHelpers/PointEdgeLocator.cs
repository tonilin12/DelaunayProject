using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PointEdgeLocator
{
    public const float MY_EPSILON = 1e-5f;
    public const int MAX_ITERATIONS = 1000;

    /// <summary>
    /// Signed 2D determinant for triangle (a,b,c) using expansion along x-column:
    /// det = x_a*(y_b - y_c) - x_b*(y_a - y_c) + x_c*(y_a - y_b)
    /// Positive => counter-clockwise, Negative => clockwise, Zero => collinear.
    /// </summary>
    public static float OrientedArea(Vertex a, Vertex b, Vertex c)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (c == null) throw new ArgumentNullException(nameof(c));

        return a.Position.X * (b.Position.Y - c.Position.Y)
             - b.Position.X * (a.Position.Y - c.Position.Y)
             + c.Position.X * (a.Position.Y - b.Position.Y);
    }

    /// <summary>
    /// Find a half-edge in the face whose endpoints match (a,b) in either orientation.
    /// Uses Face.ProcessEdges to avoid temporary lists.
    /// </summary>
    private static HalfEdge FindHalfEdgeInFace(Face face, Vertex a, Vertex b)
    {
        if (face == null) throw new ArgumentNullException(nameof(face));
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        foreach (var edge in face.ProcessEdges(e => e))
        {
            if (edge == null) continue;
            bool forward = edge.Origin.Equals(a) && edge.Dest.Equals(b);
            bool reverse = edge.Origin.Equals(b) && edge.Dest.Equals(a);
            if (forward || reverse) return edge;
        }

        return null;
    }

    /// <summary>
    /// Fill e0..e2 with the three edges of the triangular face that contains startEdge.
    /// Throws if face is not triangular.
    /// </summary>
    private static void GetTriangleEdges(HalfEdge startEdge, out HalfEdge e0, out HalfEdge e1, out HalfEdge e2)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        var face = startEdge.Face ?? throw new InvalidOperationException("HalfEdge has no face assigned.");

        e0 = startEdge;
        e1 = e0.Next ?? throw new InvalidOperationException("Malformed face: missing next edge.");
        e2 = e1.Next ?? throw new InvalidOperationException("Malformed face: missing next-next edge.");

        // Verify triangular cycle
        if (e2.Next != e0)
            throw new InvalidOperationException("Face is not triangular.");
    }

    /// <summary>
    /// Calculate the three oriented sub-areas for triangle (e0.origin, e1.origin, e2.origin) with the test point.
    /// Returns (a01, a12, a20) corresponding to orientation tests used by barycentric logic.
    /// </summary>
    public static (float a01, float a12, float a20) 
     CalculateOrientedAreas(HalfEdge startEdge, Vertex point)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        GetTriangleEdges(startEdge, out var e0, out var e1, out var e2);

        float a01 = OrientedArea(e0.Origin, e1.Origin, point);
        float a12 = OrientedArea(e1.Origin, e2.Origin, point);
        float a20 = OrientedArea(e2.Origin, e0.Origin, point);

        return (a01, a12, a20);
    }


    /// <summary>
    /// If the point lies on one of the triangle edges (within epsilon), returns that half-edge (face-local).
    /// Otherwise returns null.
    /// </summary>
    public static HalfEdge 
     GetEdgeOnWhichPointIsOn(HalfEdge startEdge, Vertex point)
    {
        var (a1, a2, a3) = CalculateOrientedAreas(startEdge, point);
        GetTriangleEdges(startEdge, out var e0, out var e1, out var e2);

        if (Math.Abs(a1) <= MY_EPSILON) return e0;
        if (Math.Abs(a2) <= MY_EPSILON) return e1;
        if (Math.Abs(a3) <= MY_EPSILON) return e2;
        return null;
    }


    /// <summary>
    /// Compute point orientation relative to triangle: (isInsideStrict, isOnEdge, nextHalfEdgeToTraverseIfOutside).
    /// </summary>
    public static (bool IsInside, bool IsOnEdge, HalfEdge NextHalfEdge) 
     GetPointOrientation(HalfEdge startEdge, Vertex point)
    {
        var (a1, a2, a3) = CalculateOrientedAreas(startEdge, point);

        bool isInside = (a1 > MY_EPSILON && a2 > MY_EPSILON && a3 > MY_EPSILON);
        bool isOnEdge = (Math.Abs(a1) <= MY_EPSILON || Math.Abs(a2) <= MY_EPSILON || Math.Abs(a3) <= MY_EPSILON);

        HalfEdge next = null;
        if (a1 < -MY_EPSILON) next = startEdge.Twin;
        else if (a2 < -MY_EPSILON) next = startEdge.Next?.Twin;
        else if (a3 < -MY_EPSILON) next = startEdge.Next?.Next?.Twin;

        return (isInside, isOnEdge, next);
    }

    /// <summary>
    /// Walk from startEdge to find the triangle containing the point or the edge on which it lies.
    /// Returns (destinationEdge, isOnEdge, traversedEdges).
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge,
                    List<HalfEdge> traversedEdges)
     LocatePointInMesh(HalfEdge startEdge, Vertex point)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        var traversed = new List<HalfEdge>();
        var visited = new HashSet<HalfEdge>();
        var current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            if (current == null)
                throw new InvalidOperationException("Encountered null half-edge during traversal.");

            if (!visited.Add(current))
                throw new InvalidOperationException("Cycle detected while traversing the mesh.");

            traversed.Add(current);

            var (isInside, isOnEdge, nextHalf) = GetPointOrientation(current, point);

            if (isInside)
                return (current, false, traversed);

            if (isOnEdge)
            {
                var edgeOn = GetEdgeOnWhichPointIsOn(current, point);
                return (edgeOn, true, traversed);
            }

            if (nextHalf == null)
                throw new InvalidOperationException("Point outside triangle but no adjacent twin found (non-manifold or boundary).");

            current = nextHalf;

            if (current == startEdge)
                return (null, false, traversed);
        }

        throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }

    /// <summary>
    /// Overload: start from a face reference.
    /// </summary>
    public static (Face destinationFace, bool isOnEdge, List<HalfEdge> traversedEdges)
     LocatePointInMesh(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        var res = LocatePointInMesh(startFace.Edge, point);
        return (res.destinationEdge?.Face, res.isOnEdge, res.traversedEdges);
    }


    /// <summary>
    /// Find the half-edge matching the geometric segment (a,b)
    /// by testing the midpoint location.
    /// Returns (foundEdge, traversalPath).
    /// </summary>
    public static (HalfEdge searchedEdge, List<HalfEdge> traversedEdges)
    FindHalfEdgeWithEdge(Face startFace, Vertex a, Vertex b)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        Vector2 mid = (a.Position + b.Position) / 2f;
        var midVertex = new Vertex(mid);

        var locate = LocatePointInMesh(startFace.Edge, midVertex);

        if (!locate.isOnEdge || locate.destinationEdge == null)
            return (null, locate.traversedEdges);

        // Search the face where the midpoint lies for the matching half-edge (either orientation)
        var found = FindHalfEdgeInFace(locate.destinationEdge.Face, a, b);
        if (found != null) return (found, locate.traversedEdges);

        var foundReverse = FindHalfEdgeInFace(locate.destinationEdge.Face, b, a);
        if (foundReverse != null) return (foundReverse, locate.traversedEdges);

        return (null, locate.traversedEdges);
    }
}
