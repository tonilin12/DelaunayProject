using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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


    public static double InCircle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        double ux = a.X - p.X;
        double uy = a.Y - p.Y;
        double uz = ux * ux + uy * uy;

        double vx = b.X - p.X;
        double vy = b.Y - p.Y;
        double vz = vx * vx + vy * vy;

        double wx = c.X - p.X;
        double wy = c.Y - p.Y;
        double wz = wx * wx + wy * wy;

        double det = ux * (vy * wz - vz * wy)
                   - uy * (vx * wz - vz * wx)
                   + uz * (vx * wy - vy * wx);

        return det;
    }

    /// <summary>
    /// Returns true if point p is inside the circumcircle of triangle ABC.
    /// </summary>
    public static bool IsInsideCircumcircle(Vertex a, Vertex b, Vertex c, Vertex p)
    {
        return InCircle(a.Position, b.Position, c.Position, p.Position) > 0;
    }

    /// <summary>
    /// Overload: Takes a Face (triangle) and a Vertex p.
    /// </summary>
    public static bool IsInsideCircumcircle(Face triangle, Vertex p)
    {
        var vertices = triangle.GetVertices().ToList();
        if (vertices.Count != 3)
            throw new ArgumentException("Face must be a triangle with 3 vertices.");

        return IsInsideCircumcircle(vertices[0], vertices[1], vertices[2], p);
    }


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
            var findpointData = PointEdgeLocator.LocatePointInMesh(currentFace.Edge, p);
            var isOnEdge = findpointData.isOnEdge;
            var searched_edge = findpointData.destinationEdge;
            var t0 = searched_edge.Face;

            List<Face> newTriangles;

            if (isOnEdge)
            {
                newTriangles = TriangulationOperation.SplitTriangleWithEdge(searched_edge, p);
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
            if (opposite_twin != null && IsInsideCircumcircle(opposite_twin.Face, p))
            {
                TriangulationOperation.FlipEdge(ref opposite_twin);

                stack.Push(triangle);
                stack.Push(opposite_twin.Face);
            }
        }
    }
}
