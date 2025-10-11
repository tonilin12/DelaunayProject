using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Linq;

public class TriangulationBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly Dictionary<int, Face> _meshTriangles; // ID-based
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

    public IEnumerable<Vertex> GetVertices()
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
        var startingTriangle = _lastInsertedTriangle ?? _meshTriangles.Values.First();
        var pointData = MeshNavigator.LocatePointInMesh(startingTriangle, vertex);
        var edge = pointData.destinationEdge;
        bool isOnEdge = pointData.isOnEdge;

        if (edge.Origin.PositionsEqual(vertex))
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

        // Fill the reusable stack for edge legalization
        _reusableStack.Clear();

        foreach (var e in vertex.GetVertexEdges())
        {
            _meshTriangles[e.Face.Id] = e.Face; // Add or update
            _reusableStack.Push(e.Next!);
        }
    }

    #region EdgeLegalization

    private delegate void EdgeFlipHandler(ref HalfEdge twin);

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

            if (stepwise)
                yield return new object();

            flipHandler(ref twin);

            _reusableStack.Push(edge.Next!);
            _reusableStack.Push(twin.Next?.Next!);

            // Ensure updated faces are in the dictionary
            _meshTriangles[edge.Face.Id] = edge.Face;
            _meshTriangles[twin.Face.Id] = twin.Face;
        }
    }

    private void LegalizeEdges(Vertex vertex)
    {
        EdgeFlipHandler immediateFlip = (ref HalfEdge twin) =>
        {
            TriangulationOperation.FlipEdge(ref twin);
        };

        foreach (var _ in ProcessEdges(vertex, immediateFlip, stepwise: false)) { }
    }

    private IEnumerable<object> EnumerateFlips(Vertex vertex)
    {
        EdgeFlipHandler deferredFlip = (ref HalfEdge twin) =>
        {
            TriangulationOperation.FlipEdge(ref twin);
        };

        return ProcessEdges(vertex, deferredFlip, stepwise: true);
    }

    #endregion
}
