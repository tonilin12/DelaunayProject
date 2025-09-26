using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using WindowsFormsApp1.myitem.GeometryFolder;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private List<Vertex> revealedPoints;
        private HashSet<Face> triangulation;
        private Face superTriangle;
        private TriangulationBuilder triangulator;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem resetMenu;
        private ToolStripMenuItem viewMenu;
        private ToolStripMenuItem showTrianglesMenuItem;
        private ToolStripMenuItem showVoronoiMenuItem;

        private Face hoveredFace = null;
        private bool hasError = false;

        // Visibility flags
        private bool showTriangles = true;
        private bool showVoronoi = true;

        public Form1()
        {
            this.Text = "Interactive Incremental Triangulation";
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);

            InitializeTriangulation();
            InitializeMenu();

            this.Paint += Form1_Paint;
            this.MouseClick += Form1_MouseClick;
            this.MouseMove += Form1_MouseMove;
            this.Load += Form1_Load;

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"Unexpected error: {e.Exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                hasError = true;
            };
        }

        private void InitializeMenu()
        {
            menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 14, FontStyle.Regular)
            };

            fileMenu = new ToolStripMenuItem("Menu");
            resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += ResetMenu_Click;
            fileMenu.DropDownItems.Add(resetMenu);

            viewMenu = new ToolStripMenuItem("View");

            showTrianglesMenuItem = new ToolStripMenuItem("Show Triangles")
            {
                Checked = true,
                CheckOnClick = true
            };
            showTrianglesMenuItem.CheckedChanged += (s, e) =>
            {
                showTriangles = showTrianglesMenuItem.Checked;
                Invalidate();
            };

            showVoronoiMenuItem = new ToolStripMenuItem("Show Voronoi")
            {
                Checked = true,
                CheckOnClick = true
            };
            showVoronoiMenuItem.CheckedChanged += (s, e) =>
            {
                showVoronoi = showVoronoiMenuItem.Checked;
                Invalidate();
            };

            viewMenu.DropDownItems.Add(showTrianglesMenuItem);
            viewMenu.DropDownItems.Add(showVoronoiMenuItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void InitializeTriangulation()
        {
            revealedPoints = new List<Vertex>();
            hasError = false;

            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            var borderPoints = new List<Vertex>
            {
                new Vertex(new Vector2(0, 0)),
                new Vertex(new Vector2(screenWidth, 0)),
                new Vertex(new Vector2(0, screenHeight)),
                new Vertex(new Vector2(screenWidth, screenHeight))
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
                MessageBox.Show($"Critical error initializing triangulation: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetMenu_Click(object sender, EventArgs e)
        {
            try
            {
                InitializeTriangulation();
                this.Invalidate();
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error resetting triangulation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (hasError) return;
            if (menuStrip.Bounds.Contains(e.Location)) return;

            try
            {
                var vertex = new Vertex(new Vector2(e.X, e.Y));
                revealedPoints.Add(vertex);

                triangulator?.AddVertices(vertex);
                triangulator?.ProcessSingleVertex();
                triangulation = triangulator?.GetInternalTriangles();

                this.Invalidate();
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error adding vertex: {ex.Message}", "Triangulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (hasError || triangulation == null) return;

            try
            {
                var p = new Vector2(e.X, e.Y);
                hoveredFace = triangulation.FirstOrDefault(face => GeometryUtils.IsPointInsideTriangle(face, p));
                Invalidate();
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error detecting hovered triangle: {ex.Message}", "Triangulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (hasError) return;

            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Pen edgePen = new Pen(Color.Blue, 2))
                using (Brush pointBrush = new SolidBrush(Color.Red))
                using (Pen circlePen = new Pen(Color.Green, 1))
                using (Pen voronoiPen = new Pen(Color.DarkOrange, 2))
                {
                    // Draw Delaunay triangles
                    if (showTriangles && triangulation != null)
                    {
                        foreach (var face in triangulation)
                        {
                            var verts = face.GetVertices().Select(v => new PointF(v.Position.X, v.Position.Y)).ToArray();
                            if (verts.Length >= 3)
                            {
                                for (int i = 0; i < verts.Length; i++)
                                {
                                    g.DrawLine(edgePen, verts[i], verts[(i + 1) % verts.Length]);
                                }
                            }
                        }
                    }

                    // Draw Voronoi polygons using each vertex's Voronoi cell
                    if (showVoronoi && revealedPoints != null)
                    {
                        foreach (var v in revealedPoints)
                        {
                            var polygon = v.Voronoi?.GetPolygon();
                            if (polygon == null || polygon.Count < 2) continue;

                            for (int i = 0; i < polygon.Count; i++)
                            {
                                var p1 = polygon[i];
                                var p2 = polygon[(i + 1) % polygon.Count];
                                g.DrawLine(voronoiPen, p1.X, p1.Y, p2.X, p2.Y);
                            }
                        }
                    }

                    // Draw vertices
                    float vertexRadius = 4;
                    foreach (var v in revealedPoints)
                    {
                        g.FillEllipse(pointBrush, v.Position.X - vertexRadius, v.Position.Y - vertexRadius, vertexRadius * 2, vertexRadius * 2);
                    }

                    // Draw circumcircle for hovered face
                    if (showTriangles && hoveredFace != null)
                    {
                        Vector2 center = hoveredFace.Circumcenter;
                        float radius = hoveredFace.Circumradius;
                        if (radius > 0f)
                        {
                            g.DrawEllipse(circlePen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical error during painting: {ex.Message}", "Render Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Optional initialization code
        }
    }
}
