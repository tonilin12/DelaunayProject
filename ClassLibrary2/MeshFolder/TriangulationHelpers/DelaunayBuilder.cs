using ClassLibrary2.MeshFolder.DataStructures;
using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class DelaunayBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly Dictionary<int, Face> _meshTriangles; // ID-based lookup
    private Face _lastInsertedTriangle;
    private readonly Stack<HalfEdge> _edgeStack = new Stack<HalfEdge>();

    public DelaunayBuilder(Face supertriangle, params Vertex[] initialVertices)
    {
        if (supertriangle == null)
            throw new ArgumentNullException(nameof(supertriangle));

        var vertices = supertriangle.GetVertices()?.ToArray()
                       ?? throw new ArgumentException("Supertriangle must have vertices.");

        if (vertices.Length != 3)
            throw new ArgumentException("Supertriangle must have exactly 3 vertices.");

        _superTriangleVertices = vertices;
        _vertexQueue = new Queue<Vertex>(initialVertices ?? Array.Empty<Vertex>());
        _meshTriangles = new Dictionary<int, Face> { { supertriangle.Id, supertriangle } };
        _lastInsertedTriangle = supertriangle;
    }

    public bool HasMoreVerticesToProcess => _vertexQueue.Count > 0;

    public void AddVertices(params Vertex[] newVertices)
    {
        if (newVertices == null || newVertices.Length == 0) return;
        foreach (var v in newVertices)
            _vertexQueue.Enqueue(v);
    }

    public IEnumerable<Face> GetInternalTriangles()
    {
        var superSet = new HashSet<Vertex>(_superTriangleVertices);
        return _meshTriangles.Values
            .Where(tri => !tri.GetVertices().Any(v => superSet.Contains(v)));
    }

    public IEnumerable<Vertex> GetInternalVertices()
    {
        var superSet = new HashSet<Vertex>(_superTriangleVertices);
        return _meshTriangles.Values
            .SelectMany(f => f.GetVertices())
            .Where(v => !superSet.Contains(v))
            .Distinct();
    }

    // -----------------------------------------------------------
    // Processing
    // -----------------------------------------------------------

    public void ProcessSingleVertex()
    {
        if (!HasMoreVerticesToProcess) return;

        Vertex vertex = _vertexQueue.Dequeue();
        AddVertexToMesh(vertex);
        LegalizeEdges(vertex);
    }

    public IEnumerable<HalfEdge> ProcessSingleVertexStepByStep()
    {
        if (!HasMoreVerticesToProcess) yield break;

        Vertex vertex = _vertexQueue.Dequeue();
        AddVertexToMesh(vertex);

        foreach (var step in LegalizeEdgeSTepBySTep(vertex))
            yield return step;
    }

    public void ProcessAllVertices()
    {
        while (HasMoreVerticesToProcess)
            ProcessSingleVertex();
    }

    // -----------------------------------------------------------
    // Insertion
    // -----------------------------------------------------------

    private void AddVertexToMesh(Vertex vertex)
    {
        var startingTriangle = _lastInsertedTriangle ?? _meshTriangles.Values.First();
        var pointData = MeshNavigator.LocatePointInMesh(startingTriangle, vertex);

        var edge = pointData.destinationEdge;
        bool isOnEdge = pointData.isOnEdge;

        // Ignore duplicates
        if (edge.Origin.PositionsEqual(vertex) || edge.Dest!.PositionsEqual(vertex))
            return;

        var containingFace = edge.Face;

        if (isOnEdge)
        {
            TriangulationOperation.SplitTriangle_VertexOnEdge(edge, vertex);
            _meshTriangles.Remove(edge.Face.Id);
            if (edge.Twin?.Face != null)
                _meshTriangles.Remove(edge.Twin.Face.Id);
        }
        else
        {
            TriangulationOperation.SplitTriangle(containingFace, vertex);
            _meshTriangles.Remove(containingFace.Id);
        }

        _lastInsertedTriangle = vertex.OutgoingHalfEdge?.Face!;

        // Prepare edge stack for legalization
        _edgeStack.Clear();

        var it = vertex.GetEdgeIterator();
        while (it.MoveNext())
        {
            var e = it.Current!;
            _meshTriangles[e.Face.Id] = e.Face; // Add or update
            _edgeStack.Push(e.Next!);
        }
    }

    // -----------------------------------------------------------
    // Edge Legalization
    // -----------------------------------------------------------

    private void LegalizeEdges(Vertex vertex)
    {
        foreach (var _ in ProcessEdges(vertex, stepwise: false)) { }
    }

    private IEnumerable<HalfEdge> LegalizeEdgeSTepBySTep(Vertex vertex)
    {
        return ProcessEdges(vertex, stepwise: true);
    }

    private IEnumerable<HalfEdge> ProcessEdges(Vertex vertex, bool stepwise)
    {
        const int iterationLimit = 10_000_000;
        int iterationCount = 0;

        while (_edgeStack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var edge = _edgeStack.Pop();
            var twin = edge.Twin;

            if (twin == null) continue;
            if (stepwise) yield return twin;

            // Check Delaunay condition
            if (GeometryUtils.InCircumcircle(twin.Face!, vertex))
            {
                TriangulationOperation.FlipEdge(twin);

                // Push newly affected edges for re-check
                if (edge.Next != null)
                    _edgeStack.Push(edge.Next);
                if (twin.Next?.Next != null)
                    _edgeStack.Push(twin.Next.Next);
            }
        }
    }




    /// <summary>
    /// Build a Voronoi cell for a given vertex by traversing its incident faces CCW.
    /// </summary>
    private VoronoiCell BuildVoronoiCell(Vertex v)
    {
        var cellvertices = new List<Vector2>();

        if (v.OutgoingHalfEdge != null)
        {
            foreach (var edge in v.GetEdges())
            {
                // Assuming each edge has a Face with a Circumcenter property of type Vector2
                if (edge?.Face != null)
                    cellvertices.Add(edge.Face.Circumcenter);
            }
        }

        return new VoronoiCell(v.Position, cellvertices);
    }

    /// <summary>
    /// Build full Voronoi diagram from triangulation.
    /// Only internal vertices (excluding supertriangle vertices).
    /// </summary>
    public List<VoronoiCell> GetVoronoi()
    {
        var cells = new List<VoronoiCell>();

        foreach (var v in GetInternalVertices())
        {
            var cell = BuildVoronoiCell(v);
            if (cell.CellVertices.Count > 0)
                cells.Add(cell);
        }

        return cells;
    }
}


