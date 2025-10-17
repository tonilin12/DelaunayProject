using ClassLibrary2.MeshFolder.Else;
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
        public readonly bool IsOnEdge;
        public readonly HalfEdge? DestinationEdge;
        public readonly HalfEdge? NextHalfEdge;

        public PointLocation( bool isOnEdge, HalfEdge? destinationEdge, HalfEdge? nextHalfEdge)
        {
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
        HalfEdge? destinationEdge = null;
        HalfEdge? nextHalfEdge = null;
        bool anyOnEdge = false;

        HalfEdge startEdge = edge.Next!;
        HalfEdge current =startEdge;

        do
        {
            float orientationValue = GeometryUtils.GetSignedArea(current.Origin, current.Dest!, point);

            // Determine next half-edge if point is on the "outside" of current edge
            if (orientationValue < -tolerance && nextHalfEdge == null)
            {
                nextHalfEdge = current.Twin;
                break;
            }

            // Check if point is on the current edge
            bool onEdge = Math.Abs(orientationValue) < tolerance && GeometryUtils.IsOnSegment(current.Origin, current.Dest, point);

            if (onEdge)
            {
                anyOnEdge = true;

                if (current.Dest.PositionsEqual(point))
                    destinationEdge = current.Next;

                if (destinationEdge == null)
                    destinationEdge = current;

                break;
            }

            current = current.Next!;
        }
        while (current != startEdge); // Stop after one full cycle around the face

        // Fallback if no destination or nextHalfEdge found
        if (nextHalfEdge == null && destinationEdge == null)
            destinationEdge = startEdge;

        return new PointLocation(anyOnEdge, destinationEdge, nextHalfEdge);
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
            if (loc.DestinationEdge!=null)
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
