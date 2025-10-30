using System;
using System.Numerics;

namespace ClassLibrary2.MeshFolder.Else
{
    public static class GeometryUtils
    {
        public static readonly float EPSILON = 1e-6f;


        #region Signed Area

        // Single point version
        public static float GetSignedArea(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float det = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
            return Math.Abs(det) < EPSILON ? 0f : det;
        }

        public static float GetSignedArea(Vertex v0, Vertex v1, Vertex v2)
            => GetSignedArea(v0.Position, v1.Position, v2.Position);

        public static float GetSignedArea(HalfEdge h0, HalfEdge h1, HalfEdge h2)
            => GetSignedArea(h0.Origin.Position, h1.Origin.Position, h2.Origin.Position);

        // Array versions
        public static float GetSignedArea(Vector2[] points)
        {
            if (points == null || points.Length != 3)
                throw new ArgumentException("Array must contain exactly 3 Vector2 points.", nameof(points));

            return GetSignedArea(points[0], points[1], points[2]);
        }

        public static float GetSignedArea(Vertex[] vertices)
        {
            if (vertices == null || vertices.Length != 3)
                throw new ArgumentException("Array must contain exactly 3 Vertex objects.", nameof(vertices));

            return GetSignedArea(vertices[0].Position, vertices[1].Position, vertices[2].Position);
        }

        public static float GetSignedArea(HalfEdge[] edges)
        {
            if (edges == null || edges.Length != 3)
                throw new ArgumentException("Array must contain exactly 3 HalfEdge objects.", nameof(edges));

            return GetSignedArea(edges[0].Origin.Position, edges[1].Origin.Position, edges[2].Origin.Position);
        }

        #endregion




        #region Circumcircle

        public static bool InCircumcircle(Vertex a, Vertex b, Vertex c, Vertex p)
        {
            float ax = a.Position.X - p.Position.X;
            float ay = a.Position.Y - p.Position.Y;
            float bx = b.Position.X - p.Position.X;
            float by = b.Position.Y - p.Position.Y;
            float cx = c.Position.X - p.Position.X;
            float cy = c.Position.Y - p.Position.Y;

            float az2 = ax * ax + ay * ay;
            float bz2 = bx * bx + by * by;
            float cz2 = cx * cx + cy * cy;

            float det = ax * (by * cz2 - bz2 * cy)
                      - ay * (bx * cz2 - bz2 * cx)
                      + az2 * (bx * cy - by * cx);

            return det > EPSILON;
        }


        public static bool InCircumcircle(Face triangle, Vertex p)
        {
            if (triangle == null)
                throw new ArgumentNullException(nameof(triangle));
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            var vertices = triangle.GetVertices();
            if (vertices == null)
                throw new InvalidOperationException("Face returned null vertices.");

            // Convert to array for fast index access
            Vertex[] vArray = vertices as Vertex[] ?? new Vertex[3];

            int count = 0;
            foreach (var v in vertices)
            {
                if (count < 3)
                    vArray[count++] = v;
                else
                    break;
            }

            if (count != 3)
                throw new ArgumentException("Face must be a triangle with exactly 3 vertices.", nameof(triangle));

            return InCircumcircle(vArray[0], vArray[1], vArray[2], p);
        }

        // Array version


        #endregion



        #region Circumcenter

        public static Vector2 Circumcenter(Vertex a, Vertex b, Vertex c)
        {
            float ax = a.Position.X, ay = a.Position.Y;
            float bx = b.Position.X, by = b.Position.Y;
            float cx = c.Position.X, cy = c.Position.Y;

            float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Math.Abs(d) < EPSILON)
                throw new InvalidOperationException("Triangle vertices are collinear, circumcenter undefined.");

            float a2 = ax * ax + ay * ay;
            float b2 = bx * bx + by * by;
            float c2 = cx * cx + cy * cy;

            float ux = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / d;
            float uy = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;

            return new Vector2(ux, uy);
        }




        #endregion




        public static bool IsOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ap = p - a;
            Vector2 ab = b - a;
            float dot = Vector2.Dot(ap, ab);

            if (dot < 0) return false;

            float lenSq = ab.LengthSquared();
            if (dot > lenSq) return false;

            return true;
        }

        public static bool IsOnSegment(Vertex a, Vertex b, Vertex p)
        {
            return IsOnSegment(a.Position, b.Position, p.Position);
        }


    }
}
