using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PointEdgeLocator
{
    public const float MY_EPSILON = 1e-5f;
    public const int MAX_ITERATIONS = 1000;

    private static HalfEdge FindHalfEdgeInFace(Face currentFace, Vertex a, Vertex b)
    {
        // Assuming ProcessEdges applies a Func<HalfEdge, HalfEdge> and returns IEnumerable<HalfEdge>
        return currentFace.ProcessEdges(edge =>
        {
            if (GeometryUtils.AreEdgesEqual((edge.Origin, edge.Dest), (a, b)))
                return edge;
            return null;
        }).FirstOrDefault(edge => edge != null);
    }

    public static (float, float, float) CalculateOrientedAreas(HalfEdge startEdge, Vertex point)
    {
        var edges = startEdge.Face.GetEdges().ToList();
        if (edges.Count != 3)
            throw new InvalidOperationException("Face is not triangular.");

        float a1 = GeometryUtils
            .OrientedArea(edges[0].Origin.Position, edges[1].Origin.Position, point.Position);
        float a2 = GeometryUtils
            .OrientedArea(edges[1].Origin.Position, edges[2].Origin.Position, point.Position);
        float a3 = GeometryUtils
            .OrientedArea(edges[2].Origin.Position, edges[0].Origin.Position, point.Position);

        return (a1, a2, a3);
    }

    public static bool IsPointInside(HalfEdge startEdge, Vertex point)
    {
        var (a1, a2, a3) = CalculateOrientedAreas(startEdge, point);
        return (a1 > MY_EPSILON && a2 > MY_EPSILON && a3 > MY_EPSILON);
    }

    public static HalfEdge GetEdgeOnWhichPointIsOn(HalfEdge startEdge, Vertex point)
    {
        var (a1, a2, a3) = CalculateOrientedAreas(startEdge, point);

        if (Math.Abs(a1) <= MY_EPSILON)
            return startEdge;          // edge 0-1
        if (Math.Abs(a2) <= MY_EPSILON)
            return startEdge.Next;     // edge 1-2
        if (Math.Abs(a3) <= MY_EPSILON)
            return startEdge.Next.Next; // edge 2-0

        return null;
    }

    /// <summary>
    /// Returns the adjacent half-edge if the point lies outside; null otherwise.
    /// </summary>
    public static HalfEdge GetNextHalfEdge(HalfEdge startEdge, Vertex point)
    {
        var (a1, a2, a3) = CalculateOrientedAreas(startEdge, point);
        var edges = startEdge.Face.GetEdges().ToList();

        if (a1 < -MY_EPSILON)
            return edges[0].Twin;
        if (a2 < -MY_EPSILON)
            return edges[1].Twin;
        if (a3 < -MY_EPSILON)
            return edges[2].Twin;

        return null;
    }

    public static (bool IsInside, bool IsOnEdge, HalfEdge NextHalfEdge) GetPointOrientation(HalfEdge startEdge, Vertex point)
    {
        HalfEdge nextEdge = GetNextHalfEdge(startEdge, point);
        bool isInside = IsPointInside(startEdge, point);
        var pointEdge = GetEdgeOnWhichPointIsOn(startEdge, point);
        return (isInside, pointEdge != null, nextEdge);
    }

    public static (HalfEdge destinationEdge, bool isOnEdge, List<HalfEdge> traversedEdges) 
    LocatePointInMesh(HalfEdge startEdge, Vertex point)
    {
        HalfEdge currentEdge = startEdge;
        List<HalfEdge> traversedEdges = new List<HalfEdge>();
        int iterations = 0;

        while (iterations++ < MAX_ITERATIONS)
        {
            traversedEdges.Add(currentEdge);

            var orientation = GetPointOrientation(currentEdge, point);

            if (orientation.IsInside)
                return (currentEdge, false, traversedEdges);

            if (orientation.IsOnEdge)
            {
                currentEdge = GetEdgeOnWhichPointIsOn(currentEdge, point);
                if (!GeometryUtils.ArePositionsEqual(currentEdge.Origin, point))
                {
                    currentEdge = currentEdge.Next;
                }
                return (currentEdge, true, traversedEdges);
            }

            currentEdge = orientation.NextHalfEdge ?? throw new Exception("Point not found in triangulation.");

            if (currentEdge == startEdge)
                return (null, false, traversedEdges);
        }

        throw new Exception("Max iterations reached while searching for point.");
    }

    public static (Face destinationFace, bool isOnEdge, List<HalfEdge> traversedEdges) LocatePointInMesh(Face startFace, Vertex point)
    {
        var data = LocatePointInMesh(startFace.Edge, point);
        return (data.destinationEdge?.Face, data.isOnEdge, data.traversedEdges);
    }

    public static (HalfEdge searchedEdge, List<HalfEdge> traversedEdges)
    FindHalfEdgeWithEdge(Face face0, Vertex a, Vertex b)
    {
        Vector2 mid = (a.Position + b.Position) / 2;
        var midVertex = new Vertex(mid);

        var findData = LocatePointInMesh(face0.Edge, midVertex);

        if (!findData.isOnEdge)
            return (null, null);

        if (findData.destinationEdge != null)
        {
            var edge0 = FindHalfEdgeInFace(findData.destinationEdge.Face, a, b);

            if (edge0 != null)
                return (edge0, findData.traversedEdges);
        }

        return (null, null);
    }
}
