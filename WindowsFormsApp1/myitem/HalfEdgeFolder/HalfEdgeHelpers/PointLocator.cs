using System;
using System.Collections.Generic;
using System.Linq;
using static WindowsFormsApp1.myitem.GeometryFolder.GeometryUtils;

public static class PointLocator
{
    private const int MAX_ITERATIONS = 1000;

    /// <summary>
    /// Stores orientation results of a point relative to a face.
    /// </summary>
    private class PointLocationResult
    {
        public bool IsInside { get; set; }
        public bool IsOnEdge { get; set; }
        public HalfEdge NextHalfEdge { get; set; }
        public List<(HalfEdge edge, int orientation)> Orientations { get; set; }
    }


    /// <summary>
    /// For a given face and point, compute orientations relative to each edge.
    /// </summary>
    private static List<(HalfEdge edge, int orientation)>
    CalculateFaceOrientations(HalfEdge startEdge, Vertex point)
    {
        if (startEdge?.Face == null)
            throw new ArgumentNullException(nameof(startEdge), "HalfEdge or its Face is null.");
        if (point == null) throw new ArgumentNullException(nameof(point));

        var result = new List<(HalfEdge, int)>();

        foreach (var e in startEdge.Face.GetEdges())
        {
            if (e.Next == null)
                throw new InvalidOperationException("Edge is missing Next.");

            int orientation = TriangleOrientation(e.Origin, e.Next.Origin, point);
            result.Add((e, orientation));
        }

        return result;
    }

    /// <summary>
    /// Compute point location relative to the face containing startEdge.
    /// </summary>
    private static PointLocationResult GetPointLocation(HalfEdge startEdge, Vertex point)
    {
        var orientations = CalculateFaceOrientations(startEdge, point);

        bool allPositive = orientations.All(o => o.orientation > 0);
        bool anyZero = orientations.Any(o => o.orientation == 0);

        return new PointLocationResult
        {
            IsInside = allPositive,
            IsOnEdge = anyZero,
            NextHalfEdge = !allPositive && !anyZero
                ? orientations.FirstOrDefault(o => o.orientation < 0).edge?.Twin
                : null,
            Orientations = orientations
        };
    }

    /// <summary>
    /// If point is exactly on an edge, return the half-edge.
    /// Prefer edge whose Origin equals the point (for vertex match).
    /// </summary>

    private static HalfEdge GetEdgeOn(PointLocationResult result, Vertex point)
    {
        HalfEdge fallback = null;

        foreach (var (edge, orientation) in result.Orientations)
        {
            // Exact vertex match: return immediately
            if (edge.Origin == point)
                return edge;

            // First edge where point is collinear, used as fallback
            if (fallback == null && orientation == 0)
                fallback = edge;
        }

        return fallback; // null if no edge found
    }

    /// <summary>
    /// Locate the face or edge containing the point in a half-edge mesh.
    /// Returns the destination edge (or null if outside) and traversal path.
    /// </summary>
    private static 
    (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
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

            var result = GetPointLocation(current, point);

            if (result.IsInside)
                return (current, false, traversed);

            if (result.IsOnEdge)
                return (GetEdgeOn(result, point), true, traversed);

            if (result.NextHalfEdge == null)
                throw new InvalidOperationException(
                    "Point outside face but no adjacent twin found (non-manifold or boundary).");

            current = result.NextHalfEdge;
        }

        throw new InvalidOperationException(
            $"Max iterations ({MAX_ITERATIONS}) reached while searching for point.");
    }


    /// <summary>
    /// Locate the face or edge containing the point in a half-edge mesh, starting from a face.
    /// Uses one of the face's half-edges and delegates to the edge-based version.
    /// </summary>
    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges)
     LocatePointInMesh(Face startFace, Vertex point)
    {
        if (startFace == null) throw new ArgumentNullException(nameof(startFace));
        if (point == null) throw new ArgumentNullException(nameof(point));
        if (startFace.Edge == null) throw new ArgumentException("The face must have a valid edge.", nameof(startFace));

        // Delegate to the edge-based version using the face's half-edge
        return LocatePointInMesh(startFace.Edge, point);
    }

}
