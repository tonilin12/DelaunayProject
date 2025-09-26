using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1.myitem.GeometryFolder
{
    public static class GeometryUtils
    {
        // Single precision constant for floating-point tolerance
        private const float EPSILON = 1e-6f;
        public static float GetEpsilon => EPSILON;


        /// <summary>
        /// Computes signed area (determinant) of a triangle.
        /// Positive = CCW, Negative = CW, Zero = Collinear
        /// </summary>
        public static float GetSignedArea(params object[] inputs)
        {
            if (inputs == null || inputs.Length != 3)
                throw new ArgumentException("Triangle must have exactly 3 vertices.", nameof(inputs));

            Vector2[] positions = inputs.Select(v =>
            {
                if (v is Vector2 vec) return vec;
                else if (v is Vertex vert) return vert.Position;
                else if (v is HalfEdge he) return he.Origin.Position;
                else throw new ArgumentException(
                    "Each element must be Vector2, Vertex, or HalfEdge.", nameof(inputs));
            }).ToArray();

            Vector2 p0 = positions[0];
            Vector2 p1 = positions[1];
            Vector2 p2 = positions[2];

            float det = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);

            // Treat extremely small numbers as zero
            if (Math.Abs(det) < EPSILON)
                det = 0f;

            return det;
        }

        /// <summary>
        /// Determines if a point is inside or on the circumcircle of a triangle.
        /// </summary>
        public static bool IsInsideOrOnCircumcircle(Face triangle, Vertex p)
        {
            if (triangle == null) throw new ArgumentNullException(nameof(triangle));
            if (p == null) throw new ArgumentNullException(nameof(p));

            var vertices = triangle.GetVertices().ToList();
            if (vertices.Count != 3)
                throw new ArgumentException("Face must be a triangle with 3 vertices.", nameof(triangle));

            Vector2 a = vertices[0].Position;
            Vector2 b = vertices[1].Position;
            Vector2 c = vertices[2].Position;
            Vector2 pt = p.Position;

            // Translate points so pt is origin
            Vector2 u = a - pt;
            Vector2 v = b - pt;
            Vector2 w = c - pt;

            float uz = u.X * u.X + u.Y * u.Y;
            float vz = v.X * v.X + v.Y * v.Y;
            float wz = w.X * w.X + w.Y * w.Y;

            // Determinant for circumcircle test
            float det = u.X * (v.Y * wz - vz * w.Y)
                      - u.Y * (v.X * wz - vz * w.X)
                      + uz * (v.X * w.Y - v.Y * w.X);

            return det >= -EPSILON;
        }

        /// <summary>
        /// Checks if a point is inside a triangle.
        /// </summary>
        public static bool IsPointInsideTriangle(Face face, Vector2 p)
        {
            var v = face.GetVertices().ToArray();
            if (v.Length != 3)
                return false;

            float d1 = GetSignedArea(p, v[0], v[1]);
            float d2 = GetSignedArea(p, v[1], v[2]);
            float d3 = GetSignedArea(p, v[2], v[0]);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        /// <summary>
        /// Computes the circumcircle of a triangle.
        /// </summary>
        public static void Circumcircle(Vertex a, Vertex b, Vertex c, out Vector2 center, out float radius)
        {
            float d = 2 * (a.Position.X * (b.Position.Y - c.Position.Y) +
                           b.Position.X * (c.Position.Y - a.Position.Y) +
                           c.Position.X * (a.Position.Y - b.Position.Y));

            float ux = ((a.Position.LengthSquared() * (b.Position.Y - c.Position.Y)) +
                        (b.Position.LengthSquared() * (c.Position.Y - a.Position.Y)) +
                        (c.Position.LengthSquared() * (a.Position.Y - b.Position.Y))) / d;

            float uy = ((a.Position.LengthSquared() * (c.Position.X - b.Position.X)) +
                        (b.Position.LengthSquared() * (a.Position.X - c.Position.X)) +
                        (c.Position.LengthSquared() * (b.Position.X - a.Position.X))) / d;

            center = new Vector2(ux, uy);
            radius = Vector2.Distance(center, a.Position);
        }
    }
}
