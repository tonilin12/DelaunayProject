using ClassLibrary2.GeometryFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

public class TriangulationBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly HashSet<Face> _meshTriangles;

    // Keep track of the last inserted triangle for fast walking
    private Face _lastInsertedTriangle;

    public TriangulationBuilder(Face supertriangle, params Vertex[] initialVertices)
    {
        if (supertriangle == null)
            throw new ArgumentNullException(nameof(supertriangle), "Supertriangle cannot be null.");

        var vertices = supertriangle.GetVertices();
        if (vertices == null || vertices.Count() != 3)
            throw new ArgumentException("Supertriangle must have exactly 3 vertices.", nameof(supertriangle));

        _superTriangleVertices = vertices.ToArray();
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
        var superVerticesSet = new HashSet<Vertex>(_superTriangleVertices);
        return GetInternalTriangles()
            .SelectMany(tri => tri.GetVertices())
            .Where(v => !superVerticesSet.Contains(v))
            .Distinct();
    }

    public void ProcessSingleVertex()
    {
        foreach (var action in ProcessSingleVertexStepByStep())
            action();
    }

    public IEnumerable<Action> ProcessSingleVertexStepByStep()
    {
        if (!HasMoreVerticesToProcess) yield break;

        Vertex vertex = _vertexQueue.Dequeue();

        // Step 1: Insert vertex and get stack for edge legalization
        Stack<HalfEdge> stack = InsertSingleVertex(vertex);

        // Yield vertex-inserted action (optional)
        yield return () => { };

        // Step 2: Yield edge flip actions
        foreach (var flip in EnumerateFlips(stack, vertex))
            yield return flip;
    }

    public void ProcessAllVertices()
    {
        while (HasMoreVerticesToProcess)
            ProcessSingleVertex();
    }

    private Stack<HalfEdge> InsertSingleVertex(Vertex vertex)
    {
        var startingTriangle = _lastInsertedTriangle ?? _meshTriangles.First();
        var pointData = MeshNavigator.LocatePointInMesh(startingTriangle, vertex);
        var edge = pointData.destinationEdge;
        bool isOnEdge = pointData.isOnEdge;

        if (edge.Origin.PositionsEqual(vertex))
            return new Stack<HalfEdge>();

        var containingFace = edge.Face;

        if (isOnEdge)
        {
            TriangulationOperation.SplitTriangle_VertexOnEdge(edge, vertex);
            _meshTriangles.Remove(edge.Face);
            if (edge.Twin?.Face != null) _meshTriangles.Remove(edge.Twin.Face);
        }
        else
        {
            TriangulationOperation.SplitTriangle(containingFace, vertex);
            _meshTriangles.Remove(containingFace);
        }

        _lastInsertedTriangle = vertex.OutgoingHalfEdge?.Face!;

        var stack = new Stack<HalfEdge>();
        foreach (var e in vertex.GetVertexEdges())
        {
            _meshTriangles.Add(e.Face);
            stack.Push(e.Next!);
        }

        return stack;
    }

    private IEnumerable<Action> EnumerateFlips(Stack<HalfEdge> stack, Vertex vertex)
    {
        int iterationLimit = 1_000_000;
        int iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var edge = stack.Pop();
            if (edge.Twin == null) continue;

            var twin = edge.Twin;
            if (GeometryUtils.IsInsideOrOnCircumcircle(twin.Face!, vertex))
            {
                yield return () => TriangulationOperation.FlipEdge(ref twin);

                stack.Push(edge.Next!);
                stack.Push(twin.Next?.Next!);
            }
        }
    }

    private void LegalizeEdges(Stack<HalfEdge> stack, Vertex vertex)
    {
        foreach (var flip in EnumerateFlips(stack, vertex))
            flip();
    }
}
