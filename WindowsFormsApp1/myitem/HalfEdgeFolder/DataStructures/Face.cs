using System;
using System.Collections.Generic;
using System.Linq;

public class Face
{
    /// <summary>
    /// A reference to one of the half-edges bordering this face.
    /// </summary>
    public HalfEdge Edge { get; set; }



    /// <summary>
    /// Constructor from three vertices (creates and links half-edges automatically).
    /// </summary>
    public Face(Vertex v1, Vertex v2, Vertex v3)
    {
        // Create three half-edges using the given vertices as origins.
        HalfEdge edge1 = new HalfEdge(v1);
        HalfEdge edge2 = new HalfEdge(v2);
        HalfEdge edge3 = new HalfEdge(v3);

        SetupEdges( new List<HalfEdge>(){edge1, edge2, edge3});
    }

    /// <summary>
    /// Constructor from three existing half-edges.
    /// </summary>
    public Face(HalfEdge edge1, HalfEdge edge2, HalfEdge edge3)
    {
        SetupEdges(new List<HalfEdge>(){edge1, edge2, edge3});
    }

    /// <summary>
    /// Constructor from a list of vertices for an arbitrary polygon.
    /// </summary>
    public Face(List<Vertex> vertices)
    {
        if (vertices == null || vertices.Count < 3)
            throw new ArgumentException("At least three vertices are required to form a face.");

        // Create a half-edge for each vertex.
        var edges = vertices.Select(v => new HalfEdge(v)).ToList();
        SetupEdges(edges);
    }

    public Face(List<HalfEdge> edges)
    {
        if (edges == null || edges.Count < 3)
            throw new ArgumentException("At least three half-edges are required to form a face.");

        SetupEdges(edges);
    }

    private void SetupEdges(List<HalfEdge> edges)
    {
        if (edges == null || edges.Count < 3)
            throw new ArgumentException("At least three edges are required to form a face.");

        // Link edges in a cycle using Next.
        for (int i = 0; i < edges.Count; i++)
        {
            HalfEdge currentEdge = edges[i];
            HalfEdge nextEdge = edges[(i + 1) % edges.Count];  // Wrap around to the first edge after the last.
            
            currentEdge.Next = nextEdge;
            nextEdge.Prev = currentEdge;

            // Set the face for each edge.
            currentEdge.Face = this;

            // Ensure that each vertex has an outgoing half-edge assigned.
            if (currentEdge.Origin.OutgoingHalfEdge == null)
                currentEdge.Origin.OutgoingHalfEdge = currentEdge;
        }

        // Assign a representative edge for the face.
        Edge = edges[0];  // You can choose any edge as the representative; typically the first one is fine.
    }

    public IEnumerable<T> ProcessEdges<T>(Func<HalfEdge, T> func)
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
    // no return version 
    // ProcessEdges<object>(e => { e.Origin.X += 1; return null!; });


    /// <summary>   
    /// Returns a list of all half-edges of this face.
    /// </summary>
    public List<HalfEdge> GetEdges()
    {
        return ProcessEdges(e => e).ToList();
    }

    /// <summary>
    /// Returns a list of all vertices of this face.
    /// </summary>
    public List<Vertex> GetVertices()
    {
        return ProcessEdges(e => e.Origin).ToList();
    }

    public List<Face> GetNeighborFaces()
    {
        return ProcessEdges(edge =>
        {
            var condition=
                edge.Twin != null && edge.Twin.Face != null
                && edge.Twin.Face != this;
                
            if (condition)
            {
                return edge.Twin.Face;
            }
            return null;
        }).Where(face => face != null).ToList(); // Convert to List
    }

    public HalfEdge GetOppositeTwinEdge(Vertex p)
    {
        return ProcessEdges(edge =>
        {
            // Check if neither the edge's origin nor the next edge's origin is the vertex p.
            if (edge.Origin != p && edge.Next.Origin != p)
            {
                return edge.Twin; // Return the neighboring face through the twin
            }
            return null; // Skip this edge
        }).FirstOrDefault(face => face != null); // Return the first valid face or null
    }

    public Vertex GetOppositeVertex(HalfEdge halfEdge)
    {
        if (halfEdge == null)
            throw new ArgumentNullException(nameof(halfEdge));
        
        // Check if the half-edge is part of this face
        if (halfEdge.Face != this)
            throw new ArgumentException("The given half-edge does not belong to this face.", nameof(halfEdge));

        // Return the opposite vertex
        return halfEdge.Next.Next.Origin;
    }

    public override string ToString()
    {
        return string.Join(" → ", GetVertices().Select(v => v.ToString()));
    }
}
