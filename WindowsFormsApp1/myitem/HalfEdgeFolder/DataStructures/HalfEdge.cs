using System;

/// <summary>
/// Represents a half-edge in a half-edge data structure.
/// </summary>
public class HalfEdge
{
    /// <summary>
    /// The vertex where this half-edge originates.
    /// </summary>
    public Vertex Origin { get; set; }

    /// <summary>
    /// Twin half-edge on the adjacent face.
    /// </summary>
    public HalfEdge Twin { get; set; }

    /// <summary>
    /// Next half-edge in the current face (counter-clockwise order).
    /// </summary>
    public HalfEdge Next { get; set; }
    public HalfEdge Prev { get; set; }



    /// <summary>
    /// The face this half-edge borders.
    /// </summary>
    public Face Face { get; set; }

    /// <summary>
    /// Destination vertex (calculated from the Next half-edge).
    /// </summary>
    public Vertex Dest => Next?.Origin;


    /// <summary>
    /// Constructor: creates a half-edge originating from a vertex.
    /// </summary>
    public HalfEdge(Vertex origin)
    {
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        if (origin.OutgoingHalfEdge is null)
        {
            origin.OutgoingHalfEdge = this;
        }
    }

    /// <summary>
    /// Returns a string representation: "Origin -> Destination".
    /// </summary>
    public override string ToString()
    {
        string destStr = Dest != null ? Dest.ToString() : "null";
        return $"{Origin} -> {destStr}";
    }

    /// <summary>
    /// Creates a pair of twin half-edges connecting 'from' to 'to'.
    /// </summary>
    public static (HalfEdge edge, HalfEdge twin)
     CreateHalfEdgePair(Vertex from, Vertex to)
    {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));

        var edge = new HalfEdge(from);

        var twin = new HalfEdge(to);

        edge.Twin = twin;
        twin.Twin = edge;

        return (edge, twin);
    }




    /// <summary>
    /// Destroys this half-edge safely and cleans up references.
    /// </summary>
    /// 
    /*
    public void Destroy()
    {
        // Break twin relationship
        if (Twin != null)
        {
            Twin.Twin = null;
            Twin = null;
        }

        // Break Next/Prev relationships
        if (Next != null)
        {
            if (Next._prev == this) Next._prev = null;
            Next = null;
        }

        if (_prev != null)
        {
            if (_prev.Next == this) _prev.Next = null;
            _prev = null;
        }

        // Remove face reference
        Face = null;

        // Remove origin reference (optional)
        Origin = null;
    }*/
}
