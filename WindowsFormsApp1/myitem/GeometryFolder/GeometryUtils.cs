using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.myitem.GeometryFolder
{
     public static class GeometryUtils
     {

        private const double EPSILON = 1e-12;



        public static int TriangleOrientation(params object[] inputs)
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

            // Compute signed area determinant
            double det = (p1.X - p0.X) * (p2.Y - p0.Y) -
                      (p1.Y - p0.Y) * (p2.X - p0.X);

            if (det > EPSILON) return 1;    // CCW
            if (det < -EPSILON) return -1;  // CW
            return 0;                        // Collinear
        }




        public static bool IsInsideCircumcircle(Face triangle, Vertex p)
        {
            if (triangle == null) throw new ArgumentNullException(nameof(triangle));
            if (p == null) throw new ArgumentNullException(nameof(p));

            var vertices = triangle.GetVertices();
            if (vertices.Count != 3)
                throw new ArgumentException("Face must be a triangle with 3 vertices.", nameof(triangle));

            Vector2 a = vertices[0].Position;
            Vector2 b = vertices[1].Position;
            Vector2 c = vertices[2].Position;
            Vector2 pt = p.Position;

            double ux = a.X - pt.X;
            double uy = a.Y - pt.Y;
            double uz = ux * ux + uy * uy;

            double vx = b.X - pt.X;
            double vy = b.Y - pt.Y;
            double vz = vx * vx + vy * vy;

            double wx = c.X - pt.X;
            double wy = c.Y - pt.Y;
            double wz = wx * wx + wy * wy;

            double det = ux * (vy * wz - vz * wy)
                       - uy * (vx * wz - vz * wx)
                       + uz * (vx * wy - vy * wx);


            return det > double.Epsilon; // using epsilon for robustness
        }

    }
}
