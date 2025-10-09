using ClassLibrary2.GeometryFolder;
using System.Numerics;

public static class MeshNavigator
{
    private const int MAX_ITERATIONS = 10_000_000;

    // ===========================
    // Lightweight result wrapper
    // ===========================
    public readonly struct PointLocation
    {
        public readonly bool IsInside;
        public readonly bool IsOnEdge;
        public readonly HalfEdge? DestinationEdge;
        public readonly HalfEdge? NextHalfEdge;

        public PointLocation(bool isInside, bool isOnEdge, HalfEdge? destinationEdge, HalfEdge? nextHalfEdge)
        {
            IsInside = isInside;
            IsOnEdge = isOnEdge;
            DestinationEdge = destinationEdge;
            NextHalfEdge = nextHalfEdge;
        }
    }

    // ===========================
    // Utility for segment check
    // ===========================
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

    // ===========================
    // Core point location logic
    // ===========================
    private static PointLocation ComputePointLocation(HalfEdge edge, Vertex point)
    {
        bool anyOnEdge = false;
        bool allPositive = true;
        HalfEdge? exactVertexEdge = null;
        HalfEdge? firstEdgeOnSegment = null;
        HalfEdge? nextHalfEdge = null;

        bool IsExactVertex(Vertex v) => v.PositionsEqual(point);
        bool IsPointOnEdge(HalfEdge e)
        {
            float orientation = GeometryUtils.GetSignedArea(e.Origin, e.Dest, point);
            return Math.Abs(orientation) < GeometryUtils.GetEpsilon && IsOnSegment(e.Origin, e.Dest, point);
        }

        // Traverse the three edges of the triangle
        HalfEdge? current = edge;
        for (int i = 0; i < 3; i++)
        {
            if (IsExactVertex(current.Origin))
                exactVertexEdge = current;

            bool onEdge = IsPointOnEdge(current);
            if (onEdge)
            {
                anyOnEdge = true;
                if (firstEdgeOnSegment == null && exactVertexEdge == null)
                    firstEdgeOnSegment = current;
            }

            float orientationValue = GeometryUtils.GetSignedArea(current.Origin, current.Dest, point);
            if (orientationValue <= 0) allPositive = false;

            if (orientationValue < 0 && nextHalfEdge == null)
                nextHalfEdge = current.Twin;

            current = current.Next;
        }

        var destination = exactVertexEdge ?? firstEdgeOnSegment;
        var nextEdge = (!allPositive && !anyOnEdge) ? nextHalfEdge : null;

        return new PointLocation(!anyOnEdge && allPositive, anyOnEdge, destination, nextEdge);
    }


    // ===========================
    // Core traversal logic
    // ===========================
    private static void TraverseEdgesCore(HalfEdge startEdge, Vertex point, Func<HalfEdge, PointLocation, bool> onEdge)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        HalfEdge current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            var location = ComputePointLocation(current, point);

            if (!onEdge(current, location))
                break;

            if (location.NextHalfEdge == null)
                throw new InvalidOperationException("Point outside face but no adjacent twin found.");

            current = location.NextHalfEdge;
        }

        if (iterations >= MAX_ITERATIONS)
            throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }

    // ===========================
    // Public API: TraverseEdges enumerable
    // ===========================
    public static IEnumerable<(HalfEdge Edge, PointLocation Location)> TraverseEdges(HalfEdge startEdge, Vertex point)
    {
        var edgesBuffer = new List<(HalfEdge, PointLocation)>();

        TraverseEdgesCore(startEdge, point,
            (edge, loc) =>
            {
                edgesBuffer.Add((edge, loc));

                // Stop if point is on vertex, on edge, or inside face
                if (loc.DestinationEdge != null && loc.DestinationEdge.Origin.PositionsEqual(point))
                    return false;

                return !(loc.IsOnEdge || loc.IsInside);
            });

        return edgesBuffer;
    }



    // ===========================
    // Public API: LocatePointInMesh
    // ===========================
    public static (HalfEdge destinationEdge, bool isOnEdge) LocatePointInMesh(HalfEdge startEdge, Vertex point)
    {
        HalfEdge? lastEdge = null;
        HalfEdge? finalDestination = null;
        bool finalIsOnEdge = false;

        TraverseEdgesCore(startEdge, point,
            (edge, loc) =>
            {
                lastEdge = edge;

                if (loc.DestinationEdge != null && loc.DestinationEdge.Origin.PositionsEqual(point))
                {
                    finalDestination = loc.DestinationEdge;
                    finalIsOnEdge = true;
                    return false;
                }

                if (loc.IsOnEdge || loc.IsInside)
                {
                    finalDestination = loc.DestinationEdge ?? edge;
                    finalIsOnEdge = loc.IsOnEdge;
                    return false;
                }

                return true;
            });

        if (finalDestination == null)
            finalDestination = lastEdge
                ?? throw new InvalidOperationException("No valid edge found.");

        return (finalDestination, finalIsOnEdge);
    }


    // ===========================
    // Overload for starting from a face
    // ===========================
    public static (HalfEdge destinationEdge, bool isOnEdge) LocatePointInMesh(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));
        if (startFace.Edge == null)
            throw new ArgumentException("Face must have a valid edge.", nameof(startFace));

        return LocatePointInMesh(startFace.Edge, point);
    }
}
