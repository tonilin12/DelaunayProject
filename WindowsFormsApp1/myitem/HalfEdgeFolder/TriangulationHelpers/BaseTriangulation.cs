using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

public class BaseTriangulation
{
    private readonly List<Vertex> _points;
    private readonly Face _supertriangle;
    private int _currentIndex;
    private Face _currentFace;
    private readonly HashSet<Face> _triangles;


    /// <summary>
    /// Tests whether point p lies inside the circumcircle of triangle ABC.
    /// Returns:
    /// > 0 if inside, 0 if on the circle, < 0 if outside.
    /// </summary>
    /// <summary>
    /// Returns the 3x3 in-circle determinant for triangle ABC and point P.
    /// Positive → inside, 0 → on the circle, negative → outside.
    /// </summary>


    public BaseTriangulation(List<Vertex> points, Face supertriangle)
    {
        _points = points ?? throw new ArgumentNullException(nameof(points));
        _supertriangle = supertriangle ?? throw new ArgumentNullException(nameof(supertriangle));

        _triangles = new HashSet<Face> { _supertriangle };
        _currentFace = _supertriangle;
        _currentIndex = 0;
    }

    /// <summary>
    /// Whether there are more points left to insert.
    /// </summary>
    public bool HasMoreSteps => _currentIndex < _points.Count;

    /// <summary>
    /// Step the triangulation by one point and return a snapshot of current triangles.
    /// </summary>
    public HashSet<Face> StepNext()
    {
        if (!HasMoreSteps)
            return new HashSet<Face>(_triangles);

        var p = _points[_currentIndex++];
        _currentFace = InsertPoint(p, _triangles, _currentFace);

        return new HashSet<Face>(_triangles); // snapshot
    }

    /// <summary>
    /// Step the triangulation by multiple points and return a snapshot.
    /// </summary>
    public HashSet<Face> Step(int count)
    {
        for (int i = 0; i < count && HasMoreSteps; i++)
        {
            StepNext();
        }
        return new HashSet<Face>(_triangles);
    }

    /// <summary>
    /// Return the current snapshot without advancing.
    /// </summary>
    public HashSet<Face> GetCurrentSnapshot()
    {
        return new HashSet<Face>(_triangles);
    }

    private Face InsertPoint(Vertex p, HashSet<Face> triangles, Face currentFace)
    {
        try
        {
            // Find containing face
            var findpointData = PointLocator.LocatePointInMesh(currentFace, p);
            var isOnEdge = findpointData.isOnEdge;
            var searched_edge = findpointData.destinationEdge;
            var t0 = searched_edge.Face;

            List<Face> newTriangles;

            if (isOnEdge)
            {
                newTriangles = TriangulationOperation.SplitTriangle_VertexOnEdge(searched_edge, p);
                triangles.Remove(searched_edge.Face);
                triangles.Remove(searched_edge.Twin.Face);
            }
            else
            {
                newTriangles = TriangulationOperation.SplitTriangle(t0, p);
                triangles.Remove(t0);
            }

            foreach (var newTriangle in newTriangles)
                triangles.Add(newTriangle);

            // Legalize edges
            LegalizeEdges(new Stack<Face>(newTriangles), p, triangles);

            return newTriangles[0];
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error inserting point {p}: {e}");
            return currentFace;
        }
    }

    private void LegalizeEdges(Stack<Face> stack, Vertex p, HashSet<Face> triangles)
    {
        int iterationLimit = 1000;
        int iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var opposite_twin = triangle.GetOppositeTwinEdge(p);
            if (opposite_twin != null && GeometryUtils.IsInsideCircumcircle(opposite_twin.Face, p))
            {
                TriangulationOperation.FlipEdge(ref opposite_twin);

                stack.Push(triangle);
                stack.Push(opposite_twin.Face);
            }
        }
    }
}
