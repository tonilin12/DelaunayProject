using MathNet.Numerics.LinearAlgebra;
using System;
using System.Numerics;

public static class GeometryUtils
{
    public static bool ArePositionsEqual(Vector2 a, Vector2 b)
    {
        return Vector2.DistanceSquared(a, b) < 0.0001f; // Tolerance for floating-point comparisons
    }

    public static bool ArePositionsEqual(Vertex a, Vertex b)
    {
        return ArePositionsEqual(a.Position, b.Position);
    }

    public static int OrientedArea(Vector2 a, Vector2 b, Vector2 c)
    {
        var matrix = Matrix<float>.Build.DenseOfArray(new float[,]
        {
            { a.X, a.Y, 1 },
            { b.X, b.Y, 1 },
            { c.X, c.Y, 1 }
        });
        return Math.Sign(matrix.Determinant());
    }

    public static int OrientedArea(Vertex a, Vertex b, Vertex c)
    {
        return OrientedArea(a.Position, b.Position, c.Position);
    }

    public static bool InCircle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var matrix = Matrix<float>.Build.DenseOfArray(new float[,]
        {
            { a.X - p.X, a.Y - p.Y, Vector2.Dot(a, a) - Vector2.Dot(p, p) },
            { b.X - p.X, b.Y - p.Y, Vector2.Dot(b, b) - Vector2.Dot(p, p) },
            { c.X - p.X, c.Y - p.Y, Vector2.Dot(c, c) - Vector2.Dot(p, p) }
        });
        return matrix.Determinant() > 0;
    }

    public static bool CheckIfConvexQuadrilateral(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 ab = b - a;
        Vector2 bc = c - b;
        Vector2 cd = d - c;
        Vector2 da = a - d;

        float crossABBC = ab.X * bc.Y - ab.Y * bc.X;
        float crossBCCD = bc.X * cd.Y - bc.Y * cd.X;
        float crossCDDA = cd.X * da.Y - cd.Y * da.X;
        float crossDAAB = da.X * ab.Y - da.Y * ab.X;

        return (crossABBC > 0 && crossBCCD > 0 && crossCDDA > 0 && crossDAAB > 0) ||
               (crossABBC < 0 && crossBCCD < 0 && crossCDDA < 0 && crossDAAB < 0);
    }

    public static bool CheckIfConvexQuadrilateral(Vertex v1, Vertex v2, Vertex v3, Vertex v4)
    {
        Vector2 a = v1.Position;
        Vector2 b = v2.Position;
        Vector2 c = v3.Position;
        Vector2 d = v4.Position;

        return CheckIfConvexQuadrilateral(a, b, c, d);
    }

    public static bool AreSegmentsCrossing(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2)
    {
        if (s1 == s2 || s1 == e2 || e1 == s2 || e1 == e2)
            return false;

        float d1 = OrientedArea(s2, e2, s1);
        float d2 = OrientedArea(s2, e2, e1);
        float d3 = OrientedArea(s1, e1, s2);
        float d4 = OrientedArea(s1, e1, e2);

        bool straddle1 = (d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0);
        bool straddle2 = (d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0);

        if (d1 == 0 && IsOnSegment(s2, e2, s1)) return true;
        if (d2 == 0 && IsOnSegment(s2, e2, e1)) return true;
        if (d3 == 0 && IsOnSegment(s1, e1, s2)) return true;
        if (d4 == 0 && IsOnSegment(s1, e1, e2)) return true;

        return straddle1 && straddle2;
    }

    public static bool IsOnSegment(Vector2 s, Vector2 e, Vector2 p)
    {
        return Math.Min(s.X, e.X) <= p.X && p.X <= Math.Max(s.X, e.X) &&
               Math.Min(s.Y, e.Y) <= p.Y && p.Y <= Math.Max(s.Y, e.Y);
    }

    public static bool AreEdgesEqual((Vertex, Vertex) edge1, (Vertex, Vertex) edge2)
    {
        var (v1a, v1b) = edge1;
        var (v2a, v2b) = edge2;

        bool sameOrder = ArePositionsEqual(v1a.Position, v2a.Position) &&
                        ArePositionsEqual(v1b.Position, v2b.Position);

        bool reverseOrder = ArePositionsEqual(v1a.Position, v2b.Position) &&
                           ArePositionsEqual(v1b.Position, v2a.Position);

        return sameOrder || reverseOrder;
    }
}