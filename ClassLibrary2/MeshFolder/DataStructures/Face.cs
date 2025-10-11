using ClassLibrary2.GeometryFolder;
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

        var edge = MakeCycle(vertices, this, v => new HalfEdge(v));
        Edge = edge;
    }

    public Face(params HalfEdge[] halfEdges)
    {
        if (halfEdges == null || halfEdges.Length != 3)
            throw new ArgumentException("Exactly 3 half-edges are required.");

        var edge = MakeCycle(halfEdges, this, e => e);
        Edge = edge;
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

        // Convert items to edges
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

        // Check for collinearity
        if (GeometryUtils.GetSignedArea(edges) == 0)
            throw new ArgumentException("Degenerate face: the three vertices are collinear.");

        // Link edges in a cycle
        for (int i = 0; i < 3; i++)
            edges[i].Next = edges[(i + 1) % 3];

        InvalidateCircumcircle();


        Id = _nextFaceId++; // assign unique ID


        return edges[0];
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


 
    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
