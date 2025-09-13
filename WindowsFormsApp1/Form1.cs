using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private List<Vertex> points;
        private HashSet<Face> triangulation;
        private Face superTriangle;
        private BaseTriangulation triangulator;

        public Form1()
        {
            this.Text = "Incremental Triangulation";
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);

            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            Debug.WriteLine("hello");

            // Original points
            points = new List<Vertex>
            {
                new Vertex(new Vector2(200, 200)),  // p0
                new Vertex(new Vector2(600, 200)),  // p1
                new Vertex(new Vector2(400, 400)),  // p2
                new Vertex(new Vector2(400, 100))   // p3
            };

            // Temporary border points for supertriangle
            List<Vertex> borderPoints = new List<Vertex>
            {
                new Vertex(new Vector2(0, 0)),                  // top-left
                new Vertex(new Vector2(screenWidth, 0)),        // top-right
                new Vertex(new Vector2(0, screenHeight)),       // bottom-left
                new Vertex(new Vector2(screenWidth, screenHeight)) // bottom-right
            };

            // Create supertriangle (unchanged)
            TriangulationOperation.getSuperTriangle(ref borderPoints, out superTriangle);

            // Initialize incremental triangulator
            triangulator = new BaseTriangulation(points, superTriangle);

            // Start incremental triangulation asynchronously
            _ = RunTriangulationAsync();

            this.Paint += Form1_Paint;
        }

        // Async incremental triangulation
        private async Task RunTriangulationAsync()
        {
            while (triangulator.HasMoreSteps)
            {
                // Step one point
                triangulation = triangulator.StepNext();

                // Force redraw
                this.Invalidate();

                // Pause between steps for visualization
                await Task.Delay(1000);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (triangulation == null) return;

            Graphics g = e.Graphics;
            Pen edgePen = new Pen(Color.Blue, 2);
            Brush pointBrush = new SolidBrush(Color.Red);

            // Draw current triangles
            foreach (var face in triangulation)
            {
                var verts = face.GetVertices()
                    .Select(v => new PointF(v.Position.X, v.Position.Y))
                    .ToArray();
                g.DrawPolygon(edgePen, verts);
            }

            // Draw points
            foreach (var v in points)
            {
                var pos = v.Position;
                g.FillEllipse(pointBrush, pos.X - 3, pos.Y - 3, 6, 6);
            }

            // Optional: draw supertriangle vertices in green
            var superVerts = superTriangle.GetVertices();
            foreach (var v in superVerts)
            {
                var pos = v.Position;
                g.FillEllipse(Brushes.Green, pos.X - 3, pos.Y - 3, 6, 6);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Optional code to run on form load
        }
    }
}
