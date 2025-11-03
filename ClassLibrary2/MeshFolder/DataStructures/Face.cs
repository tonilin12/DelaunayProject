using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            return _cachedCircumcenter!.Value;
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

        Edge = MakeCycle(vertices, this, v => new HalfEdge(v));
    }

    public Face(params HalfEdge[] halfEdges)
    {

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

            var e = toHalfEdge(item);
            e.Face = face;
            edges[index++] = e;
        }

        if (index != 3)
            throw new ArgumentException("Exactly 3 items required");

        var signedArea = GeometryUtils.GetSignedArea(edges[0].Origin, edges[1].Origin, edges[2].Origin);
        if ( Math.Abs(signedArea)==0)
            throw new ArgumentException("Degenerate face: the three vertices are collinear.");

        if (signedArea<0)
            throw new ArgumentException("Invalid orientation: face vertices must be CCW, but computed orientation is CW.");

        for (int i = 0; i < 3; i++)
            edges[i].Next = edges[(i + 1) % 3];


        // 2) If input was HalfEdge[], verify twin-side consistency (only when a twin exists)
        bool inputIsHalfEdges = typeof(T) == typeof(HalfEdge);
        if (inputIsHalfEdges)
        {
            for (int i = 0; i < 3; i++)
            {
                var e = edges[i];
                var t = e.Twin;

                if (t == null) continue;

                // Require: e.Twin.Origin == e_{i+1}.Origin
                if (!ReferenceEquals(t.Origin,e.Dest))
                    throw new InvalidOperationException("Invariant: e.Twin.Origin must equal next.Origin.");

                // Require: e.Twin.Dest == e.Origin  (i.e., t.Next?.Origin == e.Origin)
                // Only enforce when the neighbor face already wired t.Next
                if (t.Dest != null && !ReferenceEquals(t.Dest, e.Origin))
                    throw new InvalidOperationException("Invariant: e.Twin.Next.Origin must equal e.Origin.");
            }
        }


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
