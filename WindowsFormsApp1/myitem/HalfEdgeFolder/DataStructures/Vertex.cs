
using System;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic; // for HashSet




/// <summary>
/// Represents a vertex in 2D space with an optional outgoing half-edge.
/// Implements approximate equality for floating-point positions.
/// </summary>
public class Vertex : IEquatable<Vertex>
{
    public Vector2 Position { get; set; }
    public HalfEdge OutgoingHalfEdge { get; set; }

    // Tolerance stays in float, but we use double for comparisons
    public const float Tolerance = 1e-5f;

    public Vertex(Vector2 position)
    {
        Position = position;
        OutgoingHalfEdge = null;
    }

    public bool Equals(Vertex other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Cast to double for higher precision comparison
        double dx = (double)Position.X - other.Position.X;
        double dy = (double)Position.Y - other.Position.Y;

        return Math.Abs(dx) <= Tolerance && Math.Abs(dy) <= Tolerance;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Vertex);
    }


    public IEnumerable<T> EnumerateEdges<T>(Func<HalfEdge, T> func)
    {
        if (OutgoingHalfEdge == null)
            yield break;

        HalfEdge start = OutgoingHalfEdge;
        HalfEdge current = start;

        do
        {
            yield return func(current);
            current = current.Twin?.Next;
        }
        while (current != null && current != start);
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Vertex({0:F2}, {1:F2})", Position.X, Position.Y);
    }
}