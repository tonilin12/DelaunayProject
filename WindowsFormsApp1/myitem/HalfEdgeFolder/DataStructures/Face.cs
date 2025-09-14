using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApp1.myitem.GeometryFolder;

public class Face
{
    /// <summary>
    /// A reference to one of the half-edges bordering this face.
    /// </summary>
    public HalfEdge Edge { get; set; }

    /// <summary>
    /// Generic utility: creates and links a cycle of half-edges for this face.
    /// Handles both Next and Prev assignments.
    /// </summary>
    private IEnumerable<HalfEdge>
    MakeCycle<T>(IEnumerable<T> items, Face face, Func<T, HalfEdge> toHalfEdge)
    {
        if (items == null)
            throw new ArgumentException("Input items cannot be null");

        var edges = new List<HalfEdge>();

        foreach (var item in items.Distinct())
        {
            var e = toHalfEdge(item);

            // Assign face
            e.Face = face;


            edges.Add(e);
        }

        // Require at least 3 vertices
        if (edges.Count != 3)
            throw new ArgumentException("exactly 3 vertex is allowed as input");


        var p0 = edges[0].Origin.Position;
        var p1 = edges[1].Origin.Position;
        var p2 = edges[2].Origin.Position;

        // Compute twice the signed area of the triangle (p0, p1, p2)
        float area2 = (p1.X - p0.X) * (p2.Y - p0.Y) -
                      (p1.Y - p0.Y) * (p2.X - p0.X);

        // Check for collinearity
        if (GeometryUtils.TriangleOrientation(edges.ToArray())==0)
        {
            throw new ArgumentException("Degenerate face: the three vertices are collinear.");
        }

        // Properly link Next and Prev
        for (int i = 0; i < edges.Count; i++)
        {
            var current = edges[i];
            var next = edges[(i + 1) % edges.Count];

            current.Next = next;
        }

        return edges;
    }

    /// <summary>
    /// Constructor from vertices (creates new half-edges).
    /// </summary>
    public Face(params Vertex[] vertices)
    {

        var edges = MakeCycle(vertices, this, v => new HalfEdge(v)).ToList();
        Edge = edges[0];
    }

    /// <summary>
    /// Constructor from existing half-edges.
    /// </summary>
    public Face(params HalfEdge[] halfEdges)
    {

        var edges = MakeCycle(halfEdges, this, e => e).ToList();
        Edge = edges[0];
    }


    public IEnumerable<T> EnumerateEdges<T>(Func<HalfEdge, T> func)
    {
        if (Edge == null)
            yield break;

        HalfEdge start = Edge;
        HalfEdge current = start;

        do
        {
            yield return func(current);
            current = current.Next;
        }
        while (current != null && current != start);
    }


    /// <summary>
    /// Returns all vertices of this face.
    /// </summary>
    public List<Vertex> GetVertices() =>
        EnumerateEdges(e => e.Origin).ToList();

    /// <summary>
    /// Finds the opposite twin edge across
    /// the edge that does not touch vertex p.
    /// </summary>
    /// <summary>
    /// Finds the edge of this face that is opposite to vertex p.
    /// </summary>
    public HalfEdge GetOppositeEdge(Vertex p)
    {
        if (p == null) throw new ArgumentNullException(nameof(p));
        if (Edge == null) return null;

        foreach (var e in EnumerateEdges(h => h))
        {
            if (e == null || e.Next == null) continue;

            var a = e.Origin;
            var b = e.Next.Origin;

            // The edge opposite to p is the one whose vertices do NOT include p
            if (!a.PositionsEqual(p) && !b.PositionsEqual(p))
            {
                return e;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the twin of the edge opposite to vertex p, if it exists.
    /// </summary>
    public HalfEdge GetOppositeTwinEdge(Vertex p)
    {
        var oppositeEdge = GetOppositeEdge(p);
        return oppositeEdge?.Twin;
    }


    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
