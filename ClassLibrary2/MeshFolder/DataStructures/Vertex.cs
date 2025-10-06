using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

public class Vertex
{
    /// <summary>
    /// Position of the vertex in 2D space.
    /// </summary>
    public Vector2 Position { get; set; }

    private HalfEdge? _outgoingHalfEdge;

    /// <summary>
    /// Optional outgoing half-edge from this vertex.
    /// </summary>
    public HalfEdge? OutgoingHalfEdge
    {
        get => _outgoingHalfEdge;
        set => _outgoingHalfEdge = value;
    }

    /// <summary>
    /// Default tolerance for approximate comparisons.
    /// </summary>
    private readonly float Tolerance = GeometryUtils.GetEpsilon;

 

    public Vertex(float x, float y)
    {
        Position = new Vector2(x, y);
        OutgoingHalfEdge = null;
    }



    /// <summary>
    /// Enumerates half-edges originating from this vertex in CCW order, safely avoiding infinite loops.
    /// </summary>
    /// <summary>
    /// Enumerates all outgoing half-edges around this vertex in CCW order.
    /// </summary>
    public IEnumerable<HalfEdge> GetVertexEdges()
    {
        if (OutgoingHalfEdge == null)
            yield break;

        HalfEdge current = OutgoingHalfEdge;
        var visited = new HashSet<HalfEdge>();

        // Walk around the vertex counter-clockwise
        do
        {
            if (!visited.Add(current))
                yield break;  // stop if we loop back or detect a cycle

            yield return current;

            current = current.Twin?.Next;
        }
        while (current != null && current != OutgoingHalfEdge);
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
    public bool PositionsEqual(Vertex? other, float epsilon = 1e-6f)
    {
        if (other is null) return false;

        float dx = Position.X - other.Position.X;
        float dy = Position.Y - other.Position.Y;

        return dx * dx + dy * dy <= epsilon * epsilon;
    }
}
