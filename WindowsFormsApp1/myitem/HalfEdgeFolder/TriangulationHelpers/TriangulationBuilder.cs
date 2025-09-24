using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApp1.myitem.GeometryFolder;

public class TriangulationBuilder
{
    private readonly List<Vertex> _points;
    private readonly Vertex[] _supertriangleVertices;
    private int _currentIndex;

    // Single set of triangles
    private readonly HashSet<Face> _triangles;

    public TriangulationBuilder(Face supertriangle, params Vertex[] initialPoints)
    {
        if (supertriangle == null)
            throw new ArgumentNullException(nameof(supertriangle), "Supertriangle cannot be null.");

        var vertices = supertriangle.GetVertices();
        if (vertices == null || vertices.Count() != 3)
            throw new ArgumentException("Supertriangle must have exactly 3 vertices.", nameof(supertriangle));

        _supertriangleVertices = vertices.ToArray();
        _points = initialPoints?.ToList() ?? new List<Vertex>();
        _triangles = new HashSet<Face> { supertriangle };
    }

    public bool HasMoreSteps => _currentIndex < _points.Count;

    public void StepNext()
    {
        if (!HasMoreSteps) return;
        InsertVertex(_points[_currentIndex++]);
    }

    public void Step(int count)
    {
        for (int i = 0; i < count && HasMoreSteps; i++)
            StepNext();
    }

    public void AddPoints(params Vertex[] newPoints)
    {
        if (newPoints != null && newPoints.Length > 0)
            _points.AddRange(newPoints);
    }

    // Returns only internal triangles (excluding those with supertriangle vertices)
    public HashSet<Face> GetSnapshot()
    {
        return _triangles
            .Where(tri => !tri.GetVertices().Any(v => _supertriangleVertices.Contains(v)))
            .ToHashSet();
    }

    // --- Private helpers ---
    private void InsertVertex(Vertex p)
    {
        // Locate containing triangle or edge
        var findPointData = PointLocator.LocatePointInMesh(_triangles.First(), p);
        var isOnEdge = findPointData.isOnEdge;
        var edge = findPointData.destinationEdge;
        var containingFace = edge.Face;

        List<Face> newTriangles;

        if (edge.Origin.PositionsEqual(p))
        {
            // Vertex already exists
            return;
        }

        // Split triangles as needed
        if (isOnEdge)
        {
            newTriangles = TriangulationOperation.SplitTriangle_VertexOnEdge(edge, p);
            _triangles.Remove(edge.Face);
            _triangles.Remove(edge.Twin.Face);
        }
        else
        {
            newTriangles = TriangulationOperation.SplitTriangle(containingFace, p);
            _triangles.Remove(containingFace);
        }

        // Add new triangles to the set
        foreach (var tri in newTriangles)
            _triangles.Add(tri);

        // Legalize edges
        LegalizeEdges(new Stack<Face>(newTriangles), p);
    }

    private void LegalizeEdges(Stack<Face> stack, Vertex p)
    {
        int iterationLimit = 1000, iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var oppositeTwin = triangle.GetOppositeTwinEdge(p);
            if (oppositeTwin != null && GeometryUtils.IsInsideCircumcircle(oppositeTwin.Face, p))
            {
                TriangulationOperation.FlipEdge(ref oppositeTwin);

                stack.Push(triangle);
                stack.Push(oppositeTwin.Face);

                _triangles.Remove(triangle);
                _triangles.Remove(oppositeTwin.Face);

                _triangles.Add(triangle);
                _triangles.Add(oppositeTwin.Face);
            }
        }
    }
}
