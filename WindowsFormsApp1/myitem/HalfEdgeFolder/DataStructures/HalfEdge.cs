public class HalfEdge
{
    /// <summary>
    /// The vertex where this half-edge originates
    /// </summary>
    public Vertex Origin { get; set; }

    /// <summary>
    /// Twin half-edge on the adjacent face
    /// </summary>
    public HalfEdge Twin { get; set; }

    /// <summary>
    /// Next half-edge in the current face (counter-clockwise order)
    /// </summary>
    public HalfEdge Next { get; set; }

    /// <summary>
    /// The face this half-edge borders
    /// </summary>
    public Face Face { get; set; }

    /// <summary>
    /// Destination vertex (calculated via twin relationship)
    /// </summary>
    public Vertex Dest => this.Next.Origin;

    /// <summary>
    /// Previous half-edge in the current face (clockwise order)
    /// </summary>
    public HalfEdge Prev {get;set;}  // Defines Prev as this.Next.Next

    public bool IsConstrained { get; set; }

    public HalfEdge(Vertex origin)
    {
        Origin = origin;
        IsConstrained = false; // By default, the edge is not constrained
    }

    public override string ToString()
    {
        string destStr = (Next != null && Next.Origin != null) ? Next.Origin.ToString() : "null";
        return $"{Origin} -> {destStr}";
    }


    public static (HalfEdge, HalfEdge) 
    CreateHalfEdgePair(Vertex from, Vertex to)
    {
        var edge = new HalfEdge(from);
        var twin = new HalfEdge(to);
        edge.Twin = twin;
        twin.Twin = edge;
        return (edge, twin);
    }


        /// <summary>
    /// Destroys this half-edge and cleans up its associated elements.
    /// </summary>
    public void Destroy()
    {
        if (Twin != null)
        {
            // Break the twin relationship
            Twin.Twin = null;
            Twin = null;
        }

        if (Next != null)
        {
            // Remove the reference from the next half-edge's previous pointer
            Next.Prev = null;
            Next = null;
        }

        if (Prev != null)
        {
            // Remove the reference from the previous half-edge's next pointer
            Prev.Next = null;
            Prev = null;
        }

        if (Face != null)
        {
            Face = null;
        }

        // Optionally, nullify the origin if you want to free the vertex's reference as well
        Origin = null;
    }
}
