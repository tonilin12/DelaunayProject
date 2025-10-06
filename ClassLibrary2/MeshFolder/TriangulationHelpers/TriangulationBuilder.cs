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

    // 🔹 New: Keep track of the last inserted triangle for fast walking
    private Face _lastInsertedTriangle;

    // ---------------- Cache ----------------
    private IEnumerable<Face>? _cachedInternalTrianglesView;
    private bool _internalTrianglesDirty = true;

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

        _lastInsertedTriangle = supertriangle; // start walk from supertriangle
        _internalTrianglesDirty = true;
    }

    /// <summary>
    /// Checks if there are more vertices to process.
    /// </summary>
    public bool HasMoreVerticesToProcess => _vertexQueue.Count > 0;

    /// <summary>
    /// Processes a single vertex in insertion order.
    /// </summary>
    public void ProcessSingleVertex()
    {
        if (!HasMoreVerticesToProcess) return;

        Vertex vertex = _vertexQueue.Dequeue();

        Stack<Face> legalizationStack = InsertVertexIncrementally(vertex);
        LegalizeEdges(legalizationStack, vertex);

        // Mark internal triangle cache dirty
        _internalTrianglesDirty = true;
    }

    /// <summary>
    /// Processes all vertices currently in the queue (in insertion order).
    /// </summary>
    public void ProcessAllVertices()
    {
        while (HasMoreVerticesToProcess)
        {
            ProcessSingleVertex();
        }
    }

    /// <summary>
    /// Adds vertices to the queue to be processed in order.
    /// </summary>
    public void AddVertices(params Vertex[] newVertices)
    {
        if (newVertices == null || newVertices.Length == 0) return;
        foreach (var v in newVertices)
            _vertexQueue.Enqueue(v);
    }

    /// <summary>
    /// Returns a cached view of internal triangles (excluding those with supertriangle vertices).
    /// </summary>
    public IEnumerable<Face> GetInternalTriangles()
    {
        if (_cachedInternalTrianglesView == null || _internalTrianglesDirty)
        {
            _cachedInternalTrianglesView = _meshTriangles
                .Where(tri => !tri.GetVertices().Any(v => _superTriangleVertices.Contains(v)))
                .ToList(); // materialize once
            _internalTrianglesDirty = false;
        }

        return _cachedInternalTrianglesView;
    }

    public IEnumerable<Vertex> GetInternalVertices()
    {
        var superVerticesSet = new HashSet<Vertex>(_superTriangleVertices);

        // Choose the source triangles based on the condition
        IEnumerable<Face> sourceTriangles =
            (_cachedInternalTrianglesView == null && GetInternalTriangles().Any())
            ? GetInternalTriangles()
            : _meshTriangles;

        return sourceTriangles
            .SelectMany(tri => tri.GetVertices())
            .Where(v => !superVerticesSet.Contains(v))
            .Distinct();
    }


    /// <summary>
    /// Inserts a vertex into the mesh and returns the stack of new triangles for legalization.
    /// Uses _lastInsertedTriangle as starting point for walking.
    /// </summary>
    private Stack<Face> InsertVertexIncrementally(Vertex vertex)
    {
        var startingTriangle = _lastInsertedTriangle ?? _meshTriangles.First();
        var findPointData = MeshNavigator.LocatePointInMesh(startingTriangle, vertex);
        var edge = findPointData.destinationEdge;
        bool isExactlyOnEdge = findPointData.isOnEdge;

        if (edge.Origin.PositionsEqual(vertex))
            return new Stack<Face>();

        var containingFace = edge.Face;
        List<Face> newTriangles;

        if (isExactlyOnEdge)
        {
            newTriangles = TriangulationOperation.SplitTriangle_VertexOnEdge(edge, vertex);
            _meshTriangles.Remove(edge.Face);
            _meshTriangles.Remove(edge.Twin.Face);
        }
        else
        {
            newTriangles = TriangulationOperation.SplitTriangle(containingFace, vertex);
            _meshTriangles.Remove(containingFace);
        }

        foreach (var tri in newTriangles)
            _meshTriangles.Add(tri);

        _lastInsertedTriangle = newTriangles[0];

        return new Stack<Face>(newTriangles);
    }


    private IEnumerable<Action> LegalizeEdgesActions(Stack<Face> stack, Vertex vertex)
    {
        int iterationLimit = 1_000_000;
        int iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var oppositeTwin = triangle.GetOppositeTwinEdge(vertex);

            if (oppositeTwin != null && GeometryUtils.IsInsideOrOnCircumcircle(oppositeTwin.Face, vertex))
            {
                // Yield a lambda that will execute the flip when called
                yield return () => TriangulationOperation.FlipEdge(ref oppositeTwin);

                // Push back to stack to continue processing
                stack.Push(triangle);
                stack.Push(oppositeTwin.Face);
            }
        }
    }


    /// <summary>
    /// Generates a sequence of deferred flip actions for edge legalization.
    /// </summary>
    private IEnumerable<Action> PrepareLegalizeEdgeActions(Stack<Face> stack, Vertex vertex)
    {
        int iterationLimit = 1_000_000;
        int iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var oppositeTwin = triangle.GetOppositeTwinEdge(vertex);

            if (oppositeTwin != null && GeometryUtils.IsInsideOrOnCircumcircle(oppositeTwin.Face, vertex))
            {
                // Yield a lambda that will execute the flip when called
                yield return () => TriangulationOperation.FlipEdge(ref oppositeTwin);

                // Push back to stack to continue processing further flips
                stack.Push(triangle);
                stack.Push(oppositeTwin.Face);
            }
        }
    }

    /// <summary>
    /// Executes edge flips for the given stack and vertex using the prepared sequence of actions.
    /// </summary>
    private void LegalizeEdges(Stack<Face> stack, Vertex vertex)
    {
        foreach (var flipAction in PrepareLegalizeEdgeActions(stack, vertex))
        {
            flipAction(); // perform the flip
        }
    }

}
