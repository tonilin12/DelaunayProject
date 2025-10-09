using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class TriangulationBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly HashSet<Face> _meshTriangles;
    private Face _lastInsertedTriangle;
    private readonly Stack<HalfEdge> _reusableStack = new Stack<HalfEdge>();

    private static readonly Action NoOpAction = () => { };

    public TriangulationBuilder(Face supertriangle, params Vertex[] initialVertices)
    {
        if (supertriangle == null)
            throw new ArgumentNullException(nameof(supertriangle));

        var vertices = supertriangle.GetVertices()?.ToArray()
                       ?? throw new ArgumentException("Supertriangle must have vertices.");

        if (vertices.Length != 3)
            throw new ArgumentException("Supertriangle must have exactly 3 vertices.");

        _superTriangleVertices = vertices;
        _vertexQueue = new Queue<Vertex>(initialVertices ?? Array.Empty<Vertex>());
        _meshTriangles = new HashSet<Face> { supertriangle };
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
        return _meshTriangles
            .Where(tri => !tri.GetVertices().Any(v => _superTriangleVertices.Contains(v)));
    }

    public IEnumerable<Vertex> GetVertices()
    {
        var superSet = new HashSet<Vertex>(_superTriangleVertices);
        return _meshTriangles
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

        InsertSingleVertex(vertex);
        LegalizeEdges(vertex);
    }

    public IEnumerable<object> ProcessSingleVertexStepByStep()
    {
        if (!HasMoreVerticesToProcess) yield break;

        Vertex vertex = _vertexQueue.Dequeue();
        InsertSingleVertex(vertex);

        foreach (var step in EnumerateFlips(vertex))
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
    private void InsertSingleVertex(Vertex vertex)
    {
        var startingTriangle = _lastInsertedTriangle ?? _meshTriangles.First();
        var pointData = MeshNavigator.LocatePointInMesh(startingTriangle, vertex);
        var edge = pointData.destinationEdge;
        bool isOnEdge = pointData.isOnEdge;

        if (edge.Origin.PositionsEqual(vertex))
            return;

        var containingFace = edge.Face;

        if (isOnEdge)
        {
            TriangulationOperation.SplitTriangle_VertexOnEdge(edge, vertex);
            _meshTriangles.Remove(edge.Face);
            if (edge.Twin?.Face != null)
                _meshTriangles.Remove(edge.Twin.Face);
        }
        else
        {
            TriangulationOperation.SplitTriangle(containingFace, vertex);
            _meshTriangles.Remove(containingFace);
        }

        _lastInsertedTriangle = vertex.OutgoingHalfEdge?.Face!;

        // Fill the reusable stack for edge legalization
        _reusableStack.Clear();

        foreach (var e in vertex.GetVertexEdges())
        {
            _meshTriangles.Add(e.Face);
            _reusableStack.Push(e.Next!);
        }
    }

    #region EdgeLegalization

    private delegate void EdgeFlipHandler(ref HalfEdge twin);

    /// <summary>
    /// Core loop for flipping edges. Supports stepwise (pausable) or direct execution.
    /// </summary>
    private IEnumerable<object> ProcessEdges(Vertex vertex, EdgeFlipHandler flipHandler, bool stepwise)
    {
        const int iterationLimit = 1_000_000;
        int iterationCount = 0;

        while (_reusableStack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var edge = _reusableStack.Pop();
            if (edge?.Twin == null)
                continue;

            var twin = edge.Twin!;
            if (!GeometryUtils.IsInsideOrOnCircumcircle(twin.Face!, vertex))
                continue;

            // Stepwise mode: yield before executing flip
            if (stepwise)
                yield return new object();

            // Perform the flip
            flipHandler(ref twin);

            // Push edges for further legalization
            _reusableStack.Push(edge.Next!);
            _reusableStack.Push(twin.Next?.Next!);
        }
    }

    /// <summary>
    /// Fast direct legalization (all at once, no pausing)
    /// </summary>
    private void LegalizeEdges(Vertex vertex)
    {
        EdgeFlipHandler immediateFlip = (ref HalfEdge twin) =>
        {
            TriangulationOperation.FlipEdge(ref twin);
        };

        foreach (var _ in ProcessEdges(vertex, immediateFlip, stepwise: false)) { }
    }

    /// <summary>
    /// Stepwise legalization (yields placeholder before each flip)
    /// </summary>
    private IEnumerable<object> EnumerateFlips(Vertex vertex)
    {
        EdgeFlipHandler deferredFlip = (ref HalfEdge twin) =>
        {
            Debug.WriteLine("Edge to flip " + twin);
            TriangulationOperation.FlipEdge(ref twin);
        };

        return ProcessEdges(vertex, deferredFlip, stepwise: true);
    }

    #endregion
}
