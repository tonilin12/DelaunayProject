using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

public class Face
{
    private HalfEdge _edge;

    public HalfEdge Edge
    {
        get => _edge;
        set => _edge = value ?? throw new ArgumentNullException(nameof(value));
    }



    private void ComputeCircumcircle(out Vector2 center, out float radius)
    {
        var v0 = Edge.Origin;
        var v1 = Edge.Next.Origin;
        var v2 = Edge.Next.Next.Origin;

        GeometryUtils.Circumcircle(v0, v1, v2, out center, out radius);
    }

    public Vector2 Circumcenter
    {
        get
        {
            ComputeCircumcircle(out var center, out _);
            return center;
        }
    }

    public float Circumradius
    {
        get
        {
            ComputeCircumcircle(out _, out var radius);
            return radius;
        }
    }

    /// <summary>
    /// Constructor from vertices.
    /// </summary>
    public Face(params Vertex[] vertices)
    {
        if (vertices == null || vertices.Length != 3)
            throw new ArgumentException("Exactly 3 vertices are required.");

        var edges = MakeCycle(vertices, this, v => new HalfEdge(v)).ToList();
        Edge = edges[0];
    }

    /// <summary>
    /// Constructor from existing half-edges.
    /// </summary>
    public Face(params HalfEdge[] halfEdges)
    {
        if (halfEdges == null || halfEdges.Length != 3)
            throw new ArgumentException("Exactly 3 half-edges are required.");

        var edges = MakeCycle(halfEdges, this, e => e).ToList();
        Edge = edges[0];
    }

    /// <summary>
    /// Creates a linked cycle of half-edges and assigns the face.
    /// </summary>
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

        // Link Next/Prev
        for (int i = 0; i < edges.Count; i++)
        {
            edges[i].Next = edges[(i + 1) % edges.Count];
            edges[(i + 1) % edges.Count].Prev = edges[i];
        }

        return edges;
    }

    /// <summary>
    /// Enumerates all edges of the face.
    /// </summary>
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

    public IEnumerable<Vector2> GetNeighborCircumcenters()
    {
        var neighborCenters = new HashSet<Vector2>();

        foreach (var edge in GetEdges())
        {
            var twinFace = edge.Twin?.Face;
            if (twinFace != null)
            {
                neighborCenters.Add(twinFace.Circumcenter);
            }
        }

        return neighborCenters;
    }

    /// <summary>
    /// Finds the edge opposite to the given vertex.
    /// </summary>
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

    /// <summary>
    /// Finds the twin of the edge opposite to the given vertex.
    /// </summary>
    public HalfEdge GetOppositeTwinEdge(Vertex v)
    {
        return GetOppositeEdge(v)?.Twin;
    }

    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
