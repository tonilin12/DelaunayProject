using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp2.items
{
    // Renderer (instance-based)
    public class Renderer
    {
        public Color TriangleColor { get; set; } = Color.Blue;
        public Color VertexColor { get; set; } = Color.Brown;
        public Color HoverColor { get; set; } = Color.DarkSeaGreen;
        public Color VoronoiColor { get; set; } = Color.DarkOrange;

        public Renderer() { }

        public void DrawTriangles(Graphics g, HashSet<Face> triangles, int formHeight)
        {
            using var edgePen = new Pen(TriangleColor, 2);
            foreach (var face in triangles)
            {
                var verts = face.GetVertices().Select(v => new PointF(v.Position.X, formHeight - v.Position.Y)).ToArray();
                if (verts.Length < 3) continue;
                for (int i = 0; i < verts.Length; i++)
                    g.DrawLine(edgePen, verts[i], verts[(i + 1) % verts.Length]);
            }
        }

        public void DrawVertices(Graphics g, List<Vertex> vertices, int formHeight)
        {
            using var pointBrush = new SolidBrush(VertexColor);
            float radius = 4;
            foreach (var v in vertices)
            {
                float x = v.Position.X;
                float y = formHeight - v.Position.Y;
                g.FillEllipse(pointBrush, x - radius, y - radius, radius * 2, radius * 2);
            }
        }

        public void DrawHoveredTriangle(Graphics g, Face face, int formHeight, Color? circleColor = null)
        {
            if (face == null) return;

            // Draw triangle outline
            DrawHighlightedTriangle(g, face, formHeight);

            // Draw circumcircle, default green unless overridden
            DrawCircumcircle(g, face, formHeight, circleColor ?? Color.Green);
        }


        private void DrawHighlightedTriangle(Graphics g, Face face, int formHeight)
        {
            var verts = face.GetVertices()
                .Select(v => new PointF(v.Position.X, formHeight - v.Position.Y))
                .ToArray();

            if (verts.Length < 3) return;

            using var pen = new Pen(HoverColor, 4);
            for (int i = 0; i < verts.Length; i++)
                g.DrawLine(pen, verts[i], verts[(i + 1) % verts.Length]);
        }


        public void DrawCircumcircle(Graphics g, Face face, int formHeight, Color color)
        {
            Vector2 center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);

            if (r <= 0f) return;

            float centerX = center.X;
            float centerY = formHeight - center.Y;

            using var circlePen = new Pen(color, 4);
            g.DrawEllipse(circlePen, centerX - r, centerY - r, r * 2, r * 2);
        }



        public void DrawVoronoi(Graphics g, TriangulationBuilder triangulator, int formHeight)
        {
            using var voronoiPen = new Pen(VoronoiColor, 2);
            var cells = VoronoiBuilder.BuildDiagram(triangulator);
            foreach (var cell in cells)
            {
                var polygon = cell.Polygon;
                if (polygon == null || polygon.Count < 2) continue;
                for (int i = 0; i < polygon.Count; i++)
                {
                    var p1 = polygon[i];
                    var p2 = polygon[(i + 1) % polygon.Count];
                    float x1 = p1.X, y1 = formHeight - p1.Y;
                    float x2 = p2.X, y2 = formHeight - p2.Y;
                    g.DrawLine(voronoiPen, x1, y1, x2, y2);
                }
            }
        }


        public void DrawEdges(Graphics g, IEnumerable<HalfEdge> edges, int formHeight, Color color, float width = 3)
        {
            using var pen = new Pen(color, width);
            foreach (var e in edges)
            {
                if (e?.Origin == null || e?.Next?.Origin == null) continue;
                var p1 = new PointF(e.Origin.Position.X, formHeight - e.Origin.Position.Y);
                var p2 = new PointF(e.Next.Origin.Position.X, formHeight - e.Next.Origin.Position.Y);
                g.DrawLine(pen, p1, p2);
            }
        }

    }
}
