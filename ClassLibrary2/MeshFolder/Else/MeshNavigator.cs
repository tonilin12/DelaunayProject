using ClassLibrary2.GeometryFolder;
using System.Numerics;

public static class MeshNavigator
{
    private const int MAX_ITERATIONS = 10000000;

    private class PointLocationResult
    {
        public bool IsInside { get; set; }
        public bool IsOnEdge { get; set; }
        public HalfEdge? DestinationEdge { get; set; }
        public HalfEdge? NextHalfEdge { get; set; }
    }

    /// <summary>
    /// Checks if point p lies on the line segment from a to b.
    /// </summary>
    private static bool IsOnSegment(Vertex a, Vertex b, Vertex p)
    {
        // Vector from a to p
        Vector2 ap = p.Position - a.Position;

        // Vector from a to b (the segment)
        Vector2 ab = b.Position - a.Position;

        // Dot product of ap onto ab
        // Measures how far along the segment vector ab the point p projects
        float dot = Vector2.Dot(ap, ab);

        // If dot < 0, p is "behind" a, outside the segment
        if (dot < 0) return false;

        // Squared length of the segment vector
        float lenSq = ab.LengthSquared();

        // If dot > lenSq, p is "beyond" b, outside the segment
        if (dot > lenSq) return false;

        // If dot is between 0 and lenSq, p lies on the segment
        return true;
    }






    private static PointLocationResult GetPointLocation(HalfEdge startEdge, Vertex point)
    {
        bool anyOnEdge = false;
        bool allPositive = true;
        HalfEdge? exactVertexEdge = null;
        HalfEdge? firstEdgeOnSegment = null;
        HalfEdge? nextHalfEdge = null;

        Func<Vertex, bool> IsExactVertex = v => v != null && v.PositionsEqual(point);

        Func<HalfEdge, bool> IsPointOnEdge = edge =>
        {
            if (edge == null || edge.Origin == null || edge.Dest == null) return false;
            float orientation = GeometryUtils.GetSignedArea(edge.Origin, edge.Dest, point);
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

            float orientation = GeometryUtils.GetSignedArea(edge.Origin, edge.Dest, point);
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





    public static IEnumerable<HalfEdge> TraverseToPoint(HalfEdge startEdge, Vertex point)
    {
        HalfEdge current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            if (current == null)
                throw new InvalidOperationException("Encountered null half-edge during traversal.");

            yield return current;

            var result = GetPointLocation(current, point);

            if (result.DestinationEdge != null && result.DestinationEdge.Origin.PositionsEqual(point))
                yield break;

            if (result.IsOnEdge || result.IsInside)
                yield break;

            if (result.NextHalfEdge == null)
                throw new InvalidOperationException("Point outside face but no adjacent twin found.");

            current = result.NextHalfEdge;
        }

        throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }

    /// <summary>
    /// Core traversal function starting from a half-edge
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge) LocatePointInMesh(HalfEdge startEdge, Vertex point)
    {
        HalfEdge? last = null;
        PointLocationResult? finalResult = null;

        foreach (var edge in TraverseToPoint(startEdge, point))
        {
            last = edge;
            finalResult = GetPointLocation(edge, point);
        }

        if (finalResult is null)
            throw new InvalidOperationException("Traversal did not yield any edges.");

        // At this point, finalResult is not null
        HalfEdge destinationEdge = finalResult.DestinationEdge ?? last
            ?? throw new InvalidOperationException("No valid edge found.");

        bool isOnEdge =
            finalResult.IsOnEdge ||
            (finalResult.DestinationEdge != null &&
             finalResult.DestinationEdge.Origin.PositionsEqual(point));

        return (destinationEdge, isOnEdge);
    }


    /// <summary>
    /// Overload: start search from a face instead of a half-edge
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge)
    LocatePointInMesh(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));
        if (startFace.Edge == null)
            throw new ArgumentException("Face must have a valid edge.", nameof(startFace));

        return LocatePointInMesh(startFace.Edge, point);
    }

}
