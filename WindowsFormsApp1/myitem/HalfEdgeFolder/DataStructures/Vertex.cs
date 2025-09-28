using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

public class Vertex
{
    /// <summary>
    /// Position of the vertex in 2D space.
    /// </summary>
    public Vector2 Position { get; set; }

    private HalfEdge _outgoingHalfEdge;
    /// <summary>
    /// Optional outgoing half-edge from this vertex.
    /// Automatically marks Voronoi cell as dirty when modified.
    /// </summary>
    public HalfEdge OutgoingHalfEdge
    {
        get => _outgoingHalfEdge;
        set
        {
            _outgoingHalfEdge = value;
        }
    }


    /// <summary>
    /// Default tolerance for approximate comparisons.
    /// </summary>
    private float Tolerance =GeometryUtils.GetEpsilon;

    /// <summary>
    /// Constructs a vertex at the given position.
    /// </summary>
    public Vertex(Vector2 position)
    {
        Position = position;
        _outgoingHalfEdge = null;
    }

    /// <summary>
    /// Enumerates half-edges originating from this vertex in CCW order.
    /// </summary>
    public IEnumerable<T> EnumerateEdges<T>(Func<HalfEdge, T> selector, int? maxSteps = null)
    {
        if (OutgoingHalfEdge == null)
            yield break;

        HalfEdge start = OutgoingHalfEdge;
        HalfEdge current = start;
        int steps = 0;

        do
        {
            yield return selector(current);
            steps++;

            if (maxSteps.HasValue && steps >= maxSteps.Value)
                yield break;

            // Move to the next CCW edge around the vertex
            current = current.Twin?.Next;
        } while (current != null && current != start);
    }

    /// <summary>
    /// Returns a string representation of the vertex.
    /// </summary>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Vertex({0:F6}, {1:F6})", Position.X, Position.Y);
    }

    /// <summary>
    /// Checks if this vertex is approximately equal to another vertex using Euclidean distance.
    /// Float-only version.
    /// </summary>
    public bool PositionsEqual(Vertex other, float epsilon = 1e-6f)
    {
        if (other == null) return false;

        float dx = Position.X - other.Position.X;
        float dy = Position.Y - other.Position.Y;

        return dx * dx + dy * dy <= epsilon * epsilon;
    }

    /// <summary>
    /// Computes the Voronoi polygon around this vertex on the fly.
    /// Uses the circumcenters of adjacent faces.
    /// </summary>
    public List<Vector2> GetVoronoiCell()
    {
        var polygon = new List<Vector2>();

        if (OutgoingHalfEdge == null)
            return polygon;

        // Traverse all edges around the vertex in CCW order
        foreach (var center in EnumerateEdges(e => e.Face?.Circumcenter))
        {
            if (center.HasValue)
            {
                 polygon.Add(center.Value);
            }
        }

        // Close the polygon if necessary
        if (polygon.Count > 2 && Vector2.DistanceSquared(polygon[0], polygon[1]) > GeometryUtils.GetEpsilon)
            polygon.Add(polygon[0]);

        return polygon;
    }
}
