using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
// Make sure these exist and reference the correct namespaces in your project
using WindowsFormsApp1.myitem.GeometryFolder;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // Triangulation data
        private List<Vertex> revealedPoints;
        private HashSet<Face> triangulation;
        private Face superTriangle;
        private TriangulationBuilder triangulator;

        // Menu controls
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem resetMenu;

        // Hover tracking and error state
        private Face hoveredFace = null;
        private bool hasError = false;

        // Minimum distance between points
        private const float MIN_VERTEX_DISTANCE = 5f;

        public Form1()
        {
            InitializeComponent(); // Designer-generated code

            this.Text = "Interactive Incremental Triangulation";
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);

            InitializeTriangulation();
            SetupMenu();

            // Event handlers
            this.Paint += Form1_Paint;
            this.MouseClick += Form1_MouseClick;
            this.MouseMove += Form1_MouseMove;
            this.Load += Form1_Load;

            // Optional: catch unhandled thread exceptions
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"Unexpected error: {e.Exception.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                hasError = true;
            };
        }

        private void SetupMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.Font = new Font("Segoe UI", 14, FontStyle.Regular);

            fileMenu = new ToolStripMenuItem("Menu");
            resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += ResetMenu_Click;

            fileMenu.DropDownItems.Add(resetMenu);
            menuStrip.Items.Add(fileMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void InitializeTriangulation()
        {
            revealedPoints = new List<Vertex>();
            hasError = false;

            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            List<Vertex> borderPoints = new List<Vertex>
            {
                new Vertex(new Vector2(0,0)),
                new Vertex(new Vector2(screenWidth,0)),
                new Vertex(new Vector2(0,screenHeight)),
                new Vertex(new Vector2(screenWidth,screenHeight))
            };

            try
            {
                TriangulationOperation.getSuperTriangle(ref borderPoints, out superTriangle);
                triangulator = new TriangulationBuilder(superTriangle);
                triangulation = null;
                hoveredFace = null;
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error initializing triangulation: {ex.Message}", "Initialization Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetMenu_Click(object sender, EventArgs e)
        {
            InitializeTriangulation();
            this.Invalidate();
        }

        private bool IsTooClose(Vector2 newPoint)
        {
            foreach (var v in revealedPoints)
            {
                if (v.PositionEqual(new Vertex(newPoint))) return true;
                if (Vector2.Distance(v.Position, newPoint) < MIN_VERTEX_DISTANCE) return true;
            }
            return false;
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (hasError) return;
            if (menuStrip.Bounds.Contains(e.Location)) return;

            var vertex = new Vertex(new Vector2(e.X, e.Y));

            if (IsTooClose(vertex.Position)) return;

            revealedPoints.Add(vertex);

            try
            {
                triangulator?.AddVertices(vertex);
                triangulator?.ProcessSingleVertex();
                triangulation = triangulator?.GetInternalTriangles();

                this.Invalidate();
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error adding vertex: {ex.Message}", "Triangulation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (hasError || triangulation == null) return;

            var p = new Vector2(e.X, e.Y);
            hoveredFace = triangulation.FirstOrDefault(face =>
                GeometryUtils.IsPointInsideTriangle(face, p));
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (hasError) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen edgePen = new Pen(Color.Blue, 2))
            using (Brush pointBrush = new SolidBrush(Color.Red))
            using (Pen circlePen = new Pen(Color.Green, 1))
            {
                // Draw triangulation edges
                if (triangulation != null)
                {
                    foreach (var face in triangulation)
                    {
                        var verts = face.GetVertices().Select(v => new PointF(v.Position.X, v.Position.Y)).ToArray();
                        if (verts.Length >= 3) g.DrawPolygon(edgePen, verts);
                    }
                }

                // Draw vertices
                foreach (var v in revealedPoints)
                {
                    var pos = v.Position;
                    g.FillEllipse(pointBrush, pos.X - 3, pos.Y - 3, 6, 6);
                }

                // Draw circumcircle for hovered triangle
                if (hoveredFace != null)
                {
                    var verts = hoveredFace.GetVertices().ToArray();
                    if (verts.Length == 3)
                    {
                        GeometryUtils.Circumcircle(verts[0], verts[1], verts[2], out Vector2 center, out float radius);
                        g.DrawEllipse(circlePen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Optional: code on load
        }
    }
}
