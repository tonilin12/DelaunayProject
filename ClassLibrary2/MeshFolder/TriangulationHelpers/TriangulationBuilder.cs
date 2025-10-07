using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Linq;

public class TriangulationBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly HashSet<Face> _meshTriangles;

    private Face _lastInsertedTriangle;
    private readonly Stack<HalfEdge> _reusableStack = new Stack<HalfEdge>();


    // No-op action for initialization
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

    public IEnumerable<Vertex> GetInternalVertices()
    {
        var superSet = new HashSet<Vertex>(_superTriangleVertices);
        return GetInternalTriangles()
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

    public IEnumerable<Action> ProcessSingleVertexStepByStep()
    {
        if (!HasMoreVerticesToProcess) yield break;

        Vertex vertex = _vertexQueue.Dequeue();

        InsertSingleVertex(vertex);

        foreach (var flip in EnumerateFlips(vertex))
            yield return flip;
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



    #region  EdgeLegalization
    private IEnumerable<Action> EnumerateFlips(Vertex vertex)
    {
        return ProcessEdges(vertex, executeImmediately: false);
    }

    private void LegalizeEdges(Vertex vertex)
    {
        // Consume the iterator without creating Actions
        foreach (var _ in ProcessEdges(vertex, executeImmediately: true)) { }
    }

    private IEnumerable<Action> ProcessEdges(Vertex vertex, bool executeImmediately)
    {
        int iterationLimit = 1_000_000;
        int iterationCount = 0;

        // Optional initialization marker
        if (!executeImmediately)
            yield return NoOpAction;

        while (_reusableStack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var edge = _reusableStack.Pop();
            if (edge?.Twin == null)
                continue;

            if (TryFlipEdgeIfNeeded(edge, vertex, executeImmediately, out var postFlipAction))
            {
                // Push new edges for further legalization
                _reusableStack.Push(edge.Next!);
                _reusableStack.Push(edge.Twin?.Next?.Next!);

                // Yield post-flip action if not executed immediately
                if (!executeImmediately && postFlipAction != null)
                    yield return postFlipAction;
            }
        }
    }

    // Encapsulate the flip logic
    private bool TryFlipEdgeIfNeeded(HalfEdge edge, Vertex vertex, bool executeImmediately, out Action? postFlipAction)
    {
        postFlipAction = null;

        var twin = edge.Twin!;
        if (!GeometryUtils.IsInsideOrOnCircumcircle(twin.Face!, vertex))
            return false;

        if (executeImmediately)
        {
            TriangulationOperation.FlipEdge(ref twin);
        }
        else
        {
            var capturedTwin = twin;
            TriangulationOperation.FlipEdge(ref capturedTwin);

            postFlipAction = () => { /* Post-flip marker or refresh */ };
        }

        return true;
    }

    #endregion

}
