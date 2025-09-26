using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;
using WindowsFormsApp1.myitem.HalfEdgeFolder.DataStructures;

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
            Voronoi.MarkDirty(); // automatically mark dirty
        }
    }

    /// <summary>
    /// Voronoi cell associated with this vertex.
    /// </summary>
    public VoronoiCell Voronoi { get; private set; }

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
        Voronoi = new VoronoiCell(this);
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
}
