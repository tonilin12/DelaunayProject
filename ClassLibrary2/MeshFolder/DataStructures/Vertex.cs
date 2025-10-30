using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

public class Vertex
{
    public Vector2 Position { get; set; }
    private HalfEdge? _outgoingHalfEdge;

    public HalfEdge? OutgoingHalfEdge
    {
        get => _outgoingHalfEdge;
        set => _outgoingHalfEdge = value;
    }

    private readonly float Tolerance = GeometryUtils.EPSILON;

    public Vertex(float x, float y)
    {
        Position = new Vector2(x, y);
        OutgoingHalfEdge = null;
    }

    public Vertex(Vector2 position0)
    {
        Position =position0;

    }

    // ===========================================================
    //  EDGE ITERATION (USING NEW EDGEITERATOR)
    // ===========================================================

    /// <summary>
    /// Returns a lightweight iterator for traversing outgoing half-edges around this vertex.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EdgeIterator GetEdgeIterator()
    {
        return EdgeIterator.AroundVertex(this);
    }

    /// <summary>
    /// Enumerates all outgoing half-edges around this vertex in CCW order.
    /// Uses the lightweight EdgeIterator internally.
    /// </summary>
    public IEnumerable<HalfEdge> GetVertexEdges()
    {
        var iterator = GetEdgeIterator();
        while (iterator.MoveNext())
        {
            var edge = iterator.Current;
            if (edge == null) yield break;
            yield return edge;
        }
    }


    // ===========================================================
    //  OTHER METHODS
    // ===========================================================

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Vertex({0:F6}, {1:F6})", Position.X, Position.Y);
    }

    public bool PositionsEqual(Vertex? other)
    {
        if (other is null) return false;

        float dx = Position.X - other.Position.X;
        float dy = Position.Y - other.Position.Y;

        return dx * dx + dy * dy <= Tolerance * Tolerance;
    }
}
