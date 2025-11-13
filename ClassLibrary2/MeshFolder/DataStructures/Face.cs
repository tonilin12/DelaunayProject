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
    // Helper: create cycle of three half-edges
    // -----------------------------
    private HalfEdge MakeCycle<T>(
        IEnumerable<T> items,
        Face face,
        Func<T, HalfEdge> toHalfEdge)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items), "Input items cannot be null.");

        // Materialize exactly three edges
        var edges = new HalfEdge[3];
        int count = 0;

        foreach (var item in items)
        {
            if (count >= 3)
                throw new ArgumentException("Exactly 3 items are required.", nameof(items));

            var edge = toHalfEdge(item);
            edge.Face = face;
            edges[count++] = edge;
        }

        if (count != 3)
            throw new ArgumentException("Exactly 3 items are required.", nameof(items));

        // Geometry: check non-degeneracy and CCW orientation
        float signedArea = GeometryUtils.GetSignedArea(
            edges[0].Origin,
            edges[1].Origin,
            edges[2].Origin);

        if (Math.Abs(signedArea) == 0f)
            throw new ArgumentException("Degenerate face: the three vertices are collinear.", nameof(items));

        if (signedArea < 0f)
            throw new ArgumentException("Invalid orientation: face vertices must be CCW, but computed orientation is CW.", nameof(items));

        // Wire Next pointers into a CCW cycle
        for (int i = 0; i < 3; i++)
            edges[i].Next = edges[(i + 1) % 3];

        // If the input was already HalfEdge[], validate twin-side consistency
        if (typeof(T) == typeof(HalfEdge))
        {
            for (int i = 0; i < 3; i++)
            {
                var e = edges[i];
                var t = e.Twin;

                if (t == null)
                    continue;

                bool originMismatch = !ReferenceEquals(t.Origin, e.Dest);
                bool destMismatch = (t.Dest != null && !ReferenceEquals(t.Dest, e.Origin));

                if (originMismatch || destMismatch)
                {
                    string context =
                        $"[Face {Id}] Edge {i}\n" +
                        $" e: ({e.Origin} → {e.Dest}), Face={e.Face?.Id}\n" +
                        $" t: ({t.Origin} → {t.Dest}), Face={t.Face?.Id}\n";

                    string issues = "";
                    if (originMismatch)
                        issues += "- Twin.Origin ≠ e.Dest (twin edge not geometrically opposite)\n";
                    if (destMismatch)
                        issues += "- Twin.Dest ≠ e.Origin (inconsistent 'Next' linkage across faces)\n";

                    string msg =
                        context +
                        "Invariant violation detected in twin–next consistency:\n" +
                        issues +
                        "This indicates a topological mismatch between adjacent faces sharing this edge.";

                    throw new InvalidOperationException(msg);
                }
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
