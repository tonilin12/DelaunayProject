using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private List<Vertex> points;
        private HashSet<Face> triangulation;
        private Face superTriangle;

        public Form1()
        {
            this.Text = "Draw Triangle";
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);
            points = new List<Vertex>
            {
                new Vertex(new Vector2(200, 200)),  // p0
                new Vertex(new Vector2(600, 200)),  // p1
                new Vertex(new Vector2(400, 400)),  // p2 (top vertex)
                new Vertex(new Vector2(400, 100))   // p3 (bottom vertex, inside circumcircle of p0-p1-p2)
            };

            List<(Vertex, Vertex)> edgeConstraints = new List<(Vertex, Vertex)>();
            TriangulationOperation.PrepareData(ref points, ref edgeConstraints, out superTriangle);
            triangulation = TriangulationOperation.GetTriangulation(points, superTriangle);

            this.Paint += Form1_Paint;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen edgePen = new Pen(Color.Blue, 2);
            Brush pointBrush = new SolidBrush(Color.Red);

            foreach (var face in triangulation)
            {
                var verts = face.GetVertices()
                    .Select(v => new PointF(v.Position.X, v.Position.Y))
                    .ToArray();
                g.DrawPolygon(edgePen, verts);
            }

            foreach (var v in points)
            {
                var pos = v.Position;
                g.FillEllipse(pointBrush, pos.X - 3, pos.Y - 3, 6, 6);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}

