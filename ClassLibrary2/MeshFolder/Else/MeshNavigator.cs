using ClassLibrary2.MeshFolder.Else;
using System.Numerics;

public static class MeshNavigator
{
    private const int MAX_ITERATIONS = 10_000_000;

    // ===========================
    // Lightweight result wrapper
    // ===========================
    public readonly struct MeshTraversalStep
    {
        public readonly bool IsOnEdge;
        public readonly HalfEdge? DestinationEdge;
        public readonly HalfEdge? NextHalfEdge;

        public MeshTraversalStep( bool isOnEdge, HalfEdge? destinationEdge, HalfEdge? nextHalfEdge)
        {
            IsOnEdge = isOnEdge;
            DestinationEdge = destinationEdge;
            NextHalfEdge = nextHalfEdge;
        }
    }



    // ===========================
    // Core point location logic
    // ===========================
    private static MeshTraversalStep EvaluateNextStep(HalfEdge edge, Vertex point)
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
            if (orientationValue <0 && nextHalfEdge == null)
            {
                nextHalfEdge = current.Twin;
                break;
            }

            // Check if point is on the current edge
            bool onEdge = Math.Abs(orientationValue)==0 && GeometryUtils.IsOnSegment(current.Origin, current.Dest!, point);

            if (onEdge)
            {
                anyOnEdge = true;

                if (current.Dest!.PositionsEqual(point))
                    destinationEdge = current.Next;


                break;
            }

            current = current.Next!;
        }
        while (current != startEdge); // Stop after one full cycle around the face

        // Fallback if no destination or nextHalfEdge found
        if (nextHalfEdge == null && destinationEdge == null)
            destinationEdge =current;

        return new MeshTraversalStep(anyOnEdge, destinationEdge, nextHalfEdge);
    }


    // ===========================
    // Locate point from edge
    // ===========================
    public static (HalfEdge destinationEdge, bool isOnEdge)
    LocatePointInMesh(HalfEdge startEdge, Vertex point, Action<MeshTraversalStep>? onPointLocation = null)
    {
        if (startEdge == null) throw new ArgumentNullException(nameof(startEdge));
        if (point == null) throw new ArgumentNullException(nameof(point));

        HalfEdge current = startEdge;
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            var loc = EvaluateNextStep(current, point);


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
