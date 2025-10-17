using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Face
{
    private HalfEdge _edge;

    private static int _nextFaceId = 0; // global counter for all faces
    public int Id { get; private set; } // unique ID for this face

    public HalfEdge Edge
    {
        get => _edge;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            _edge = value;
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
            return;

        var e0 = Edge;
        var e1 = e0?.Next;
        var e2 = e1?.Next;

        var v0 = e0.Origin;
        var v1 = e1.Origin;
        var v2 = e2.Origin;

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

        Edge = MakeCycle(vertices, this, v => new HalfEdge(v));
    }

    public Face(params HalfEdge[] halfEdges)
    {
        if (halfEdges == null || halfEdges.Length != 3)
            throw new ArgumentException("Exactly 3 half-edges are required.");

        Edge = MakeCycle(halfEdges, this, e => e);
    }

    // -----------------------------
    // Helper: create cycle of half-edges
    // -----------------------------
    private HalfEdge MakeCycle<T>(IEnumerable<T> items, Face face, Func<T, HalfEdge> toHalfEdge)
    {
        if (items == null)
            throw new ArgumentException("Input items cannot be null");

        var edges = new HalfEdge[3];
        int index = 0;

        foreach (var item in items)
        {
            if (index >= 3)
                throw new ArgumentException("Exactly 3 items required");

            var e = toHalfEdge(item);
            e.Face = face;
            edges[index++] = e;
        }

        if (index != 3)
            throw new ArgumentException("Exactly 3 items required");

        if (GeometryUtils.GetSignedArea(edges) == 0)
            throw new ArgumentException("Degenerate face: the three vertices are collinear.");

        for (int i = 0; i < 3; i++)
            edges[i].Next = edges[(i + 1) % 3];

        InvalidateCircumcircle();
        Id = _nextFaceId++;

        return edges[0];
    }


    public EdgeIterator GetEdgeIterator()
    {
        return EdgeIterator.AroundFace(this);
    }

    // -----------------------------
    // Enumerators using EdgeIterator
    // -----------------------------
    public IEnumerable<HalfEdge> GetEdges()
    {
        var it =GetEdgeIterator();
        while (it.MoveNext())
        {
            yield return it.Current!;
        }
    }

    public IEnumerable<Vertex> GetVertices()
    {
        var it = EdgeIterator.AroundFace(this);
        while (it.MoveNext())
        {
            yield return it.Current!.Origin;
        }
    }


    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
