using System;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic;

/// <summary>
/// Represents a vertex in 2D space with an optional outgoing half-edge.
/// Safe reference equality by default, with explicit approximate comparison helper.
/// </summary>
public class Vertex
{
    /// <summary>
    /// Position of the vertex in 2D space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Optional outgoing half-edge from this vertex.
    /// </summary>
    public HalfEdge OutgoingHalfEdge { get; set; }

    /// <summary>
    /// Default tolerance for approximate comparisons.
    /// </summary>
    public const float Tolerance = 1e-5f;

    /// <summary>
    /// Constructs a vertex at the given position.
    /// </summary>
    public Vertex(Vector2 position)
    {
        Position = position;
        OutgoingHalfEdge = null;
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
    public bool PositionsEqual(Vertex other, float tolerance = Tolerance)
    {
        if (other == null) return false;

        float dx = Position.X - other.Position.X;
        float dy = Position.Y - other.Position.Y;

        return dx * dx + dy * dy <= tolerance * tolerance;
    }
}
