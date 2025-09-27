using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

public class TriangulationBuilder
{
    private readonly Queue<Vertex> _vertexQueue;
    private readonly Vertex[] _superTriangleVertices;
    private readonly HashSet<Face> _meshTriangles;

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
    /// Returns internal triangles (excluding those with supertriangle vertices).
    /// </summary>
    public HashSet<Face> GetInternalTriangles()
    {
        return _meshTriangles
            .Where(tri => !tri.GetVertices().Any(v => _superTriangleVertices.Contains(v)))
            .ToHashSet();
    }




    /// <summary>
    /// Inserts a vertex into the mesh and returns the stack of new triangles for legalization.
    /// </summary>
    private Stack<Face> InsertVertexIncrementally(Vertex vertex)
    {
        var findPointData = PointLocator.LocatePointInMesh(_meshTriangles.First(), vertex);
        var edge = findPointData.destinationEdge;

        // Separate flags
        bool isExactlyOnEdge = findPointData.isOnEdge;


        // Vertex coincides with an existing edge vertex → skip insertion
        if (edge.Origin.PositionsEqual(vertex))
        {
            return new Stack<Face>();
        }

        var containingFace = edge.Face;
        List<Face> newTriangles;



        if (isExactlyOnEdge)
        {
            // Split two adjacent triangles along the edge
            newTriangles = TriangulationOperation.SplitTriangle_VertexOnEdge(edge, vertex);
            _meshTriangles.Remove(edge.Face);
            _meshTriangles.Remove(edge.Twin.Face);
        }
        else
        {
            // Insert vertex inside a single triangle
            newTriangles = TriangulationOperation.SplitTriangle(containingFace, vertex);
            _meshTriangles.Remove(containingFace);
        }


        foreach (var tri in newTriangles)
            _meshTriangles.Add(tri);


        return new Stack<Face>(newTriangles);
    }


    /// <summary>
    /// Legalizes triangle edges using a stack-based incremental approach.
    /// </summary>
    private void LegalizeEdges(Stack<Face> stack, Vertex vertex)
    {
        int iterationLimit = 1000, iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var oppositeTwin = triangle.GetOppositeTwinEdge(vertex);
            if (oppositeTwin != null && GeometryUtils.IsInsideOrOnCircumcircle(oppositeTwin.Face, vertex))
            {
                TriangulationOperation.FlipEdge(ref oppositeTwin);

                // Push affected triangles back for further legalization
                stack.Push(triangle);
                stack.Push(oppositeTwin.Face);
            }
        }
    }
}
