
using System;
using System.Globalization;
using System.Numerics;
using System.Collections.Generic; // for HashSet



using System;

/// <summary>
/// Represents a vertex in 2D space with an optional outgoing half-edge.
/// Implements approximate equality for floating-point positions.
/// </summary>
public class Vertex : IEquatable<Vertex>
{
    public Vector2 Position { get; set; }
    public HalfEdge OutgoingHalfEdge { get; set; }

    // Make Tolerance public so tests can access it
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

        return Math.Abs(Position.X - other.Position.X) <= Tolerance &&
               Math.Abs(Position.Y - other.Position.Y) <= Tolerance;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Vertex);
    }

    public override int GetHashCode()
    {
        int xHash = (int)Math.Round(Position.X / Tolerance);
        int yHash = (int)Math.Round(Position.Y / Tolerance);

        return (xHash * 397) ^ yHash;
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Vertex({0:F2}, {1:F2})", Position.X, Position.Y);
    }
}
