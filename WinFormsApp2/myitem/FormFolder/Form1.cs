using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.myitem.GeometryFolder;
using ClassLibrary2;


namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly object _lock = new object(); // lock for thread-safety
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

        private bool showTriangles = true;
        private bool showVoronoi = true;

        private const int VertexProcessTimeoutMs = 500;

        public Form1()
        {
            this.Text = "Interactive Incremental Triangulation";
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);

            InitializeMenu();
            InitializeTriangulation();

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
            menuStrip = new MenuStrip();
            menuStrip.Font = new Font("Segoe UI", 14, FontStyle.Regular);

            fileMenu = new ToolStripMenuItem("File");
            resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += ResetMenu_Click;
            fileMenu.DropDownItems.Add(resetMenu);

            viewMenu = new ToolStripMenuItem("View");

            showTrianglesMenuItem = new ToolStripMenuItem("Show Triangles", null, (s, e) =>
            {
                showTriangles = !showTriangles;
                showTrianglesMenuItem.Checked = showTriangles;
                Invalidate();
            })
            { Checked = showTriangles };

            showVoronoiMenuItem = new ToolStripMenuItem("Show Voronoi", null, (s, e) =>
            {
                showVoronoi = !showVoronoi;
                showVoronoiMenuItem.Checked = showVoronoi;
                Invalidate();
            })
            { Checked = showVoronoi };

            viewMenu.DropDownItems.Add(showTrianglesMenuItem);
            viewMenu.DropDownItems.Add(showVoronoiMenuItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void InitializeTriangulation()
        {
            hasError = false;

            lock (_lock)
            {
                // Remove references to previous mesh; GC will reclaim if nothing else references them
                triangulation?.Clear();
                triangulation = null;
                triangulator = null;
                hoveredFace = null;
                superTriangle = null;

                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                var borderPoints = new Vertex[]
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
                    // triangulation stays null until we add vertices
                }
                catch (Exception ex)
                {
                    hasError = true;
                    MessageBox.Show($"Critical error initializing triangulation: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ResetMenu_Click(object sender, EventArgs e)
        {
            try
            {
                if (hasError)
                {
                    // Hard reset: restart application (guaranteed clean slate)
                    System.Diagnostics.Process.Start(Application.ExecutablePath);
                    Application.Exit();
                }
                else
                {
                    // Soft reset: reinitialize state in this process
                    SoftReset();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during reset: {ex.Message}", "Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SoftReset()
        {
            lock (_lock)
            {
                // Clear references to allow GC to reclaim memory eventually
                if (triangulation != null)
                    triangulation.Clear();
                triangulation = null;

                hoveredFace = null;

                triangulator = null;


                hasError = false;

                // Recreate base triangulation state
                InitializeTriangulation();

                // Redraw UI
                Invalidate();
            }
        }

        private async Task ProcessVertexWithTimeoutAsync(Vertex vertex, int timeoutMs = VertexProcessTimeoutMs)
        {
            Exception caughtException = null;
            using (var cts = new CancellationTokenSource())
            {
                var worker = Task.Run(() =>
                {
                    try
                    {
                        lock (_lock)
                        {
                            triangulator?.AddVertices(vertex);
                            triangulator?.ProcessSingleVertex();
                            triangulation = triangulator?.GetInternalTriangles();
                        }
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                }, cts.Token);

                var completed = await Task.WhenAny(worker, Task.Delay(timeoutMs));
                if (completed != worker)
                {
                    // timeout
                    try { cts.Cancel(); } catch { }
                    throw new TimeoutException($"Triangulation processing exceeded {timeoutMs} ms.");
                }

                // ensure any exceptions are observed
                await worker;

               //throw new InvalidOperationException("Test exception - startup failed.");


                if (caughtException != null)
                    throw new InvalidOperationException("Error during triangulation: " + caughtException.Message, caughtException);
            }
        }

        private async void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (hasError) return;
            if (menuStrip.Bounds.Contains(e.Location)) return;

            var vertex = new Vertex(new Vector2(e.X, e.Y));

            try
            {
                await ProcessVertexWithTimeoutAsync(vertex);
                // triangulation updated inside the worker; repaint now
                Invalidate();
            }
            catch (TimeoutException ex)
            {
                hasError = true;
                MessageBox.Show(ex.Message, "Triangulation Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                hasError = true;
                MessageBox.Show($"Critical triangulation error: {ex.Message}", "Triangulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            // quick bailouts - do not lock unless we have triangulation
            if (hasError || triangulation == null) return;

            var p = new Vector2(e.X, e.Y);

            lock (_lock)
            {
                // safe read of triangulation under lock
                hoveredFace = triangulation.FirstOrDefault(face => GeometryUtils.IsPointInsideTriangle(face, p));
            }
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (hasError) return;

            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // All drawing is done on UI thread; lock to read triangulation/triangulator safely
            lock (_lock)
            {
                using (Pen edgePen = new Pen(Color.Blue, 2))
                using (Brush pointBrush = new SolidBrush(Color.Red))
                using (Pen circlePen = new Pen(Color.Green, 1))
                using (Pen voronoiPen = new Pen(Color.DarkOrange, 2))
                {
                    if (showTriangles && triangulation != null)
                    {
                        foreach (var face in triangulation)
                        {
                            var vert0 = face.GetVertices().ToList();
                            var verts = vert0.Select(v => new PointF(v.Position.X, v.Position.Y)).ToArray();

                            if (verts.Length < 3) continue;

                            for (int i = 0; i < verts.Length; i++)
                                graphics.DrawLine(edgePen, verts[i], verts[(i + 1) % verts.Length]);
                        }
                    }

                    var internalVertices = triangulator?.GetInternalVertices();
                    if (showVoronoi && internalVertices != null)
                    {
                        foreach (var v in internalVertices)
                        {
                            var polygon = v.GetVoronoiCell();
                            if (polygon == null || polygon.Count < 2) continue;

                            for (int i = 0; i < polygon.Count; i++)
                            {
                                var p1 = polygon[i];
                                var p2 = polygon[(i + 1) % polygon.Count];
                                graphics.DrawLine(voronoiPen, p1.X, p1.Y, p2.X, p2.Y);
                            }

                        }
                    }

                    if (internalVertices != null)
                    {
                        float vertexRadius = 3;
                        foreach (var v in internalVertices)
                            graphics.FillEllipse(pointBrush, v.Position.X - vertexRadius, v.Position.Y - vertexRadius, vertexRadius * 2, vertexRadius * 2);
                    }

                    if (showTriangles && hoveredFace != null)
                    {
                        // Access to hoveredFace is under lock, so safe.
                        Vector2 center = hoveredFace.Circumcenter;
                        float radius = Vector2.Distance(center, hoveredFace.GetVertices().First().Position);
                        if (radius > 0f)
                            graphics.DrawEllipse(circlePen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // optional additional initialization
        }
    }
}
