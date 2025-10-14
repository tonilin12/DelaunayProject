using ClassLibrary2.GeometryFolder;
using System.Numerics;

public static class MeshNavigator
{
    private const int MAX_ITERATIONS = 10_000_000;
    private static readonly float tolerance = GeometryUtils.GetEpsilon;

    // ===========================
    // Lightweight result wrapper
    // ===========================
    public readonly struct PointLocation
    {
        public readonly bool DestinationReached;
        public readonly bool IsOnEdge;
        public readonly HalfEdge? DestinationEdge;
        public readonly HalfEdge? NextHalfEdge;

        public PointLocation(bool destinationReached, bool isOnEdge, HalfEdge? destinationEdge, HalfEdge? nextHalfEdge)
        {
            DestinationReached = destinationReached;
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

        HalfEdge current = edge;
        for (int i = 0; i < 3; i++)
        {
            float orientationValue = GeometryUtils.GetSignedArea(current.Origin, current.Dest, point);

            bool onEdge = Math.Abs(orientationValue) < tolerance && IsOnSegment(current.Origin, current.Dest, point);

            if (onEdge)
            {
                if (IsExactVertex(current.Origin))
                    exactVertexEdge = current;

                anyOnEdge = true;
                if (firstEdgeOnSegment == null && exactVertexEdge == null)
                    firstEdgeOnSegment = current;
            }

            if (orientationValue <= 0) allPositive = false;

            if (orientationValue < 0 && nextHalfEdge == null)
                nextHalfEdge = current.Twin;

            current = current.Next!;
        }

        // Determine destination edge safely
        HalfEdge? destinationEdge = null;
        if (anyOnEdge)
            destinationEdge = exactVertexEdge ?? firstEdgeOnSegment;
        else if (allPositive)
            destinationEdge = edge;

        HalfEdge? nextEdge = (!allPositive && !anyOnEdge) ? nextHalfEdge : null;

        return new PointLocation(destinationEdge != null, anyOnEdge, destinationEdge, nextEdge);
    }

    // ===========================
    // Locate point from edge
    // ===========================
    public static (HalfEdge destinationEdge, bool isOnEdge)
    LocatePointInMesh(HalfEdge startEdge, Vertex point, Action<PointLocation>? onPointLocation = null)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        HalfEdge current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            var loc = ComputePointLocation(current, point);


            // Destination reached
            if (loc.DestinationReached)
                return (loc.DestinationEdge!, loc.IsOnEdge);

            // Cannot continue
            if (loc.NextHalfEdge == null)
                throw new InvalidOperationException("Point outside face but no adjacent twin found.");



            // Invoke optional callback for every step
            onPointLocation?.Invoke(loc);

            current = loc.NextHalfEdge;
        }

        throw new InvalidOperationException($"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }


    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> allNextTwins)
    LocatePointAndLogTraverse(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));


        var startEdge = startFace.Edge ?? throw new ArgumentException("Face must have a valid edge.", nameof(startFace));
        var allNextTwins = new List<HalfEdge>();

        // Call the existing LocatePointInMesh with a callback
        var (destinationEdge, isOnEdge) = LocatePointInMesh(startEdge, point, loc =>
        {
            // Collect the NextHalfEdge twin if it exists
            if (loc.NextHalfEdge != null)
                allNextTwins.Add(loc.NextHalfEdge.Twin!);
        });



        return (destinationEdge, isOnEdge, allNextTwins);
    }



    // ===========================
    // Locate point from face
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
