using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Face
{
    private HalfEdge _edge;

    public HalfEdge Edge
    {
        get => _edge;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            _edge = value;

            // Invalidate cached circumcircle and mark Voronoi dirty
            InvalidateCircumcircle();
        }
    }

    // -----------------------------
    // Cached circumcircle
    // -----------------------------
    private Vector2? _cachedCircumcenter = null;

    private void ComputeCircumcircle()
    {
        if (_cachedCircumcenter.HasValue)
            return; // Already computed

        var v0 = Edge.Origin;
        var v1 = Edge?.Next?.Origin;
        var v2 = Edge?.Next?.Next?.Origin;

        _cachedCircumcenter = GeometryUtils.Circumcenter(v0, v1, v2);

    }

    public Vector2 Circumcenter
    {
        get
        {
            ComputeCircumcircle();
            return _cachedCircumcenter.Value;
        }
    }


    /// <summary>
    /// Invalidate cached circumcircle (call if vertices or edges move)
    /// </summary>
    public void InvalidateCircumcircle()
    {
        _cachedCircumcenter = null;
    }

    // -----------------------------
    // Constructors
    // -----------------------------
    public Face(params Vertex[] vertices)
    {
        if (vertices == null || vertices.Length != 3)
            throw new ArgumentException("Exactly 3 vertices are required.");

        var edges = MakeCycle(vertices, this, v => new HalfEdge(v)).ToList();
        Edge = edges[0];
    }

    public Face(params HalfEdge[] halfEdges)
    {
        if (halfEdges == null || halfEdges.Length != 3)
            throw new ArgumentException("Exactly 3 half-edges are required.");

        var edges = MakeCycle(halfEdges, this, e => e).ToList();
        Edge = edges[0];
    }

    // -----------------------------
    // Helper: create cycle of half-edges
    // -----------------------------
    private IEnumerable<HalfEdge> MakeCycle<T>(IEnumerable<T> items, Face face, Func<T, HalfEdge> toHalfEdge)
    {
        if (items == null)
            throw new ArgumentException("Input items cannot be null");

        var edges = new List<HalfEdge>();

        foreach (var item in items.Distinct())
        {
            var e = toHalfEdge(item);
            e.Face = face;
            edges.Add(e);
        }

        if (edges.Count != 3)
            throw new ArgumentException("Exactly 3 vertices/edges required for a triangular face.");

        if (GeometryUtils.GetSignedArea(edges.ToArray()) == 0)
            throw new ArgumentException("Degenerate face: the three vertices are collinear.");

        for (int i = 0; i < edges.Count; i++)
        {
            var curr = edges[i];
            var next = edges[(i + 1) % 3];

            curr.Next = next;

        }

        // Invalidate cached circumcircle after linking edges
        InvalidateCircumcircle();

        return edges;
    }

    // -----------------------------
    // Enumerators
    // -----------------------------
    private IEnumerable<HalfEdge> EnumerateEdges()
    {
        if (Edge == null) yield break;

        var start = Edge;
        var current = start;
        do
        {
            yield return current;
            current = current.Next;
        } while (current != null && current != start);
    }

    public IEnumerable<HalfEdge> GetEdges() => EnumerateEdges();

    public IEnumerable<Vertex> GetVertices()
    {
        foreach (var e in EnumerateEdges())
            yield return e.Origin;
    }

    // -----------------------------
    // Opposite edge utilities
    // -----------------------------
    public HalfEdge GetOppositeEdge(Vertex v)
    {
        if (v == null) throw new ArgumentNullException(nameof(v));

        foreach (var e in GetEdges())
        {
            var a = e.Origin;
            var b = e.Next.Origin;
            if (!a.PositionsEqual(v) && !b.PositionsEqual(v))
                return e;
        }

        return null;
    }

    public HalfEdge GetOppositeTwinEdge(Vertex v)
    {
        return GetOppositeEdge(v).Twin;
    }

    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
