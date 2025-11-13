using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WinFormsApp2.items
{
    // Renderer (instance-based)
    public class Renderer
    {
        // Base
        public Color TriangleColor { get; set; } = Color.RoyalBlue;
        public Color VertexColor { get; set; } = Color.MidnightBlue;
        public Color VoronoiColor { get; set; } = Color.DarkOrange;

        // Hover palettes (independent of flip colors)
        public Color OutlineColor { get; set; } = Color.SkyBlue; // default hover
        public Color HoverCircleColor { get; set; } = Color.CadetBlue;

        public Color FlipNormalCircleColor { get; set; } = Color.Green;
        public Color FlipHappenedCircleColor { get; set; } = Color.Crimson;

        public Renderer() { }

        // ---------------- Triangles ----------------
        public void DrawTriangles(Graphics g, List<Face> triangles, int formHeight)
        {
            if (triangles == null || triangles.Count == 0) return;

            using var pen = MakePen(TriangleColor, 2f);
            foreach (var face in triangles)
            {
                var verts = face.GetVertices()
                                .Select(v => new PointF(v.Position.X, formHeight - v.Position.Y))
                                .ToArray();
                if (verts.Length < 3) continue;
                for (int i = 0; i < verts.Length; i++)
                    g.DrawLine(pen, verts[i], verts[(i + 1) % verts.Length]);
            }
        }

        // ---------------- Vertices ----------------
        public void DrawVertices(Graphics g, List<Vertex> vertices, int formHeight)
        {
            if (vertices == null || vertices.Count == 0) return;
            const float r = 4f;

            foreach (var v in vertices)
            {
                float x = v.Position.X;
                float y = formHeight - v.Position.Y;

                using (var glow = new SolidBrush(Color.FromArgb(100, VertexColor)))
                    g.FillEllipse(glow, x - r * 2, y - r * 2, r * 4, r * 4);

                using (var dot = new SolidBrush(VertexColor))
                    g.FillEllipse(dot, x - r, y - r, r * 2, r * 2);

                using var outline = MakePen(Color.White, 1f);
                g.DrawEllipse(outline, x - r, y - r, r * 2, r * 2);
            }
        }

        // ---------------- Hover (Face under mouse) ----------------
        public void DrawHoverFace(Graphics g, Face face, int formHeight, bool isFlipActive = false)
        {
            if (face == null) return;

            var circle = HoverCircleColor;
            DrawFaceOutline(g, face, formHeight, OutlineColor, 4f);
            DrawCircumcircle(g, face, formHeight, circle);
        }

        // ---------------- Flip (edge-associated face) ----------------
        public void DrawEdgeFace(Graphics g, HalfEdge edge, int formHeight, bool flipHappened)
        {
            if (edge?.Face == null) return;

            var circle = flipHappened ? FlipHappenedCircleColor : FlipNormalCircleColor;


            // Highlight current edge

            DrawFaceOutline(g, edge.Face, formHeight, OutlineColor, 4f);

            DrawCircumcircle(g, edge.Face, formHeight, circle);
        }

        // ---------------- Voronoi ----------------
        public void DrawVoronoi(Graphics g, DelaunayBuilder triangulator, int formHeight)
        {
            if (triangulator == null) return;

            using var pen = MakePen(VoronoiColor, 2f);
            var cells = triangulator.GetVoronoi();
            foreach (var cell in cells)
            {
                var poly = cell.CellVertices;
                if (poly == null || poly.Count < 2) continue;
                for (int i = 0; i < poly.Count; i++)
                {
                    var a = poly[i];
                    var b = poly[(i + 1) % poly.Count];
                    g.DrawLine(pen, new PointF(a.X, formHeight - a.Y), new PointF(b.X, formHeight - b.Y));
                }
            }
        }

        // ---------------- Vertex incident edges ----------------
        public void DrawEdgesForVertex(Graphics g, Vertex vertex, int formHeight, Color color, float width = 3f)
        {
            if (vertex == null) return;

            using var glowPen = MakePen(color, width, glowColor: Color.Yellow, glowAlpha: 128, glowScale: 2.5f);
            using var mainPen = MakePen(color, width);

            foreach (var e in vertex.GetEdges().Select(x => x.Next))
            {
                var p1 = new PointF(e.Origin.Position.X, formHeight - e.Origin.Position.Y);
                var p2 = new PointF(e.Dest.Position.X, formHeight - e.Dest.Position.Y);
                g.DrawLine(glowPen, p1, p2);
                g.DrawLine(mainPen, p1, p2);
            }
        }

        // ---------------- Helpers ----------------
        private void DrawFaceOutline(Graphics g, Face face, int formHeight, Color color, float width)
        {
            var verts = face.GetVertices()
                            .Select(v => new PointF(v.Position.X, formHeight - v.Position.Y))
                            .ToArray();
            if (verts.Length < 3) return;

            using var pen = MakePen(color, width);
            for (int i = 0; i < verts.Length; i++)
                g.DrawLine(pen, verts[i], verts[(i + 1) % verts.Length]);
        }

        private void DrawCircumcircle(Graphics g, Face face, int formHeight, Color color)
        {
            var center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);
            if (r <= 0f) return;

            float cx = center.X;
            float cy = formHeight - center.Y;

            using var pen = MakePen(color, 4f);
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
        }

        private static Pen MakePen(Color color, float width, Color? glowColor = null, int glowAlpha = 128, float glowScale = 2.5f)
        {
            bool hasGlow = glowColor.HasValue;
            Color finalColor = hasGlow ? Color.FromArgb(glowAlpha, glowColor.Value) : color;
            float finalWidth = hasGlow ? width * glowScale : width;

            return new Pen(finalColor, finalWidth)
            {
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
        }

        public void DrawHalfEdge(Graphics g, HalfEdge edge, int formHeight, Color color, float width = 3f)
        {
            if (edge == null || edge.Origin == null || edge.Dest == null) return;

            var p1 = new PointF(edge.Origin.Position.X, formHeight - edge.Origin.Position.Y);
            var p2 = new PointF(edge.Dest.Position.X, formHeight - edge.Dest.Position.Y);

            using var pen = MakePen(color, width);
            g.DrawLine(pen, p1, p2);
        }
    }
}
