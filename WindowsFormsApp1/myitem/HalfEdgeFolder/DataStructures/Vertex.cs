using System;
using System.Numerics;

public class Vertex : IEquatable<Vertex>
{
    public Vector2 Position { get; set; }
    public HalfEdge OutgoingHalfEdge { get; set; }
    private const float Epsilon = 0.0001f; // Precision threshold

    public Vertex(Vector2 position)
    {
        Position = position;
        OutgoingHalfEdge = null;
    }

    // Clone method to create a new instance with the same Position
    public Vertex ShallowCopy()
    {
        return new Vertex(Position);
    }

    // Override Equals to compare Position with tolerance
    public override bool Equals(object obj)
    {
        return Equals(obj as Vertex);
    }

    // Implements IEquatable<Vertex> with floating point precision tolerance
    public bool Equals(Vertex other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Use GeometryUtils method for approximate equality
        return GeometryUtils.ArePositionsEqual(this.Position, other.Position);
    }

    // Override GetHashCode to use Position's hash code
    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }

    public override string ToString()
    {
        return $"Vertex({Position.X:F2}, {Position.Y:F2})";
    }
}
