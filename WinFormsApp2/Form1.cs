using ClassLibrary2;
using ClassLibrary2.GeometryFolder;
using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly object _lock = new object(); // lock for thread-safety
        private HashSet<Face>? triangulation;
        private Face superTriangle;
        private TriangulationBuilder? triangulator;
        private List<Vertex> insertedVertices = new List<Vertex>();

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem resetMenu;
        private ToolStripMenuItem exportObjMenu;
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

        // ---------------------------
        // Menu Initialization
        // ---------------------------
        private void InitializeMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.Font = new Font("Segoe UI", 14, FontStyle.Regular);

            fileMenu = new ToolStripMenuItem("File");
            resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += ResetMenu_Click;

            // Export OBJ option
            exportObjMenu = new ToolStripMenuItem("Export Triangulation (.obj)");
            exportObjMenu.Click += ExportObjMenu_Click;

            fileMenu.DropDownItems.Add(resetMenu);
            fileMenu.DropDownItems.Add(exportObjMenu);

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

        // ---------------------------
        // Triangulation Initialization
        // ---------------------------
        private void InitializeTriangulation()
        {
            hasError = false;

            lock (_lock)
            {
                triangulation?.Clear();
                triangulation = null;
                triangulator = null;
                hoveredFace = null;
                superTriangle = null;
                insertedVertices.Clear();

                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                var borderPoints = new Vertex[]
                {
                    new Vertex(0, 0),
                    new Vertex(screenWidth, 0),
                    new Vertex(0, screenHeight),
                    new Vertex(screenWidth, screenHeight)
                };

                try
                {
                    TriangulationOperation.getSuperTriangle(ref borderPoints, out superTriangle);
                    triangulator = new TriangulationBuilder(superTriangle);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    MessageBox.Show($"Critical error initializing triangulation: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ---------------------------
        // Reset
        // ---------------------------
        private void ResetMenu_Click(object sender, EventArgs e)
        {
            try
            {
                if (hasError)
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath);
                    Application.Exit();
                }
                else
                {
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
                triangulation?.Clear();
                triangulation = null;

                hoveredFace = null;
                triangulator = null;
                insertedVertices.Clear();
                hasError = false;

                InitializeTriangulation();
                Invalidate();
            }
        }

        // ---------------------------
        // Vertex Processing
        // ---------------------------
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
                            HashSet<Face>? faces = triangulator?.GetInternalTriangles().ToHashSet();
                            triangulation = faces;

                            // Store insertion order
                            insertedVertices.Add(vertex);
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
                    try { cts.Cancel(); } catch { }
                    throw new TimeoutException($"Triangulation processing exceeded {timeoutMs} ms.");
                }

                await worker;

                if (caughtException != null)
                    throw new InvalidOperationException("Error during triangulation: " + caughtException.Message, caughtException);
            }
        }

        // ---------------------------
        // Mouse Events
        // ---------------------------
        private async void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (hasError) return;
            if (menuStrip.Bounds.Contains(e.Location)) return;

            var vertex = new Vertex(e.X, e.Y);

            try
            {
                await ProcessVertexWithTimeoutAsync(vertex);
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
            if (hasError || triangulation == null) return;

            var p = new Vector2(e.X, e.Y);

            lock (_lock)
            {
                hoveredFace = triangulation.FirstOrDefault(face => GeometryUtils.IsPointInsideTriangle(face, p));
            }
            Invalidate();
        }

        // ---------------------------
        // Paint
        // ---------------------------
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (hasError) return;

            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            lock (_lock)
            {
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
                            if (verts.Length < 3) continue;

                            for (int i = 0; i < verts.Length; i++)
                                graphics.DrawLine(edgePen, verts[i], verts[(i + 1) % verts.Length]);
                        }
                    }

                    // Draw Voronoi diagram
                    if (showVoronoi && triangulator != null)
                    {
                        var voronoiCells = VoronoiBuilder.BuildDiagram(triangulator);

                        foreach (var cell in voronoiCells)
                        {
                            var polygon = cell.Polygon;
                            if (polygon == null || polygon.Count < 2) continue;

                            for (int i = 0; i < polygon.Count; i++)
                            {
                                var p1 = polygon[i];
                                var p2 = polygon[(i + 1) % polygon.Count];
                                graphics.DrawLine(voronoiPen, p1.X, p1.Y, p2.X, p2.Y);
                            }
                        }
                    }

                    // Draw vertices
                    if (triangulator != null)
                    {
                        float vertexRadius = 3;
                        foreach (var v in insertedVertices)
                            graphics.FillEllipse(pointBrush, v.Position.X - vertexRadius, v.Position.Y - vertexRadius, vertexRadius * 2, vertexRadius * 2);
                    }

                    // Highlight hovered triangle
                    if (showTriangles && hoveredFace != null)
                    {
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
            // optional initialization
        }

        // ---------------------------
        // Export OBJ + Insertion Log
        // ---------------------------
        private void ExportObjMenu_Click(object sender, EventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    if (triangulation == null || triangulation.Count == 0 || insertedVertices.Count == 0)
                    {
                        MessageBox.Show("No triangulation data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // --- Create export folder with timestamp ---
                    string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string exportDir = Path.Combine(projectDir, "Exports", $"Triangulation_{timestamp}");
                    Directory.CreateDirectory(exportDir);

                    // --- File paths ---
                    string objFilePath = Path.Combine(exportDir, "triangulation.obj");
                    string logFilePath = Path.Combine(exportDir, "insertion_log.txt");

                    // --- Export triangulation and insertion log ---
                    ExportTriangulationToObj(objFilePath); // Uses the corrected OBJ export method
                    ExportInsertionLog(logFilePath);

                    // --- Feedback to the user ---
                    MessageBox.Show(
                        $"Triangulation export finished successfully!\n\nOBJ file: {objFilePath}\nInsertion log: {logFilePath}",
                        "Export Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export triangulation: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ExportTriangulationToObj(string filePath)
        {
            lock (_lock)
            {
                if (triangulation == null || triangulation.Count == 0)
                {
                    MessageBox.Show("No triangulation data to export.", "Export OBJ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (var writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("# Triangulation OBJ Export");
                        writer.WriteLine("# Generated by Interactive Incremental Triangulation");

                        // --- Step 1: Collect unique vertices from triangulation faces ---
                        var allVerts = triangulation
                            .SelectMany(f => f.GetVertices())            // flatten all face vertices
                            .ToList();

                        // Group by position to get unique vertices (tuple uses exact float equality;
                        // that's OK here since your points are created from integer screen coords)
                        var vertexList = allVerts
                            .GroupBy(v => (v.Position.X, v.Position.Y))
                            .Select(g => g.First())
                            .OrderBy(v => v.Position.X)
                            .ThenBy(v => v.Position.Y)
                            .ToList();

                        // --- Step 2: Build mapping from position -> OBJ index ---
                        var vertexIndices = new Dictionary<(float x, float y), int>();
                        for (int i = 0; i < vertexList.Count; i++)
                        {
                            var v = vertexList[i];
                            var key = (v.Position.X, v.Position.Y);
                            vertexIndices[key] = i + 1; // OBJ indices start at 1

                            // Use InvariantCulture to force dot decimal separator
                            writer.WriteLine(
                                $"v {v.Position.X.ToString("F6", CultureInfo.InvariantCulture)} " +
                                $"{v.Position.Y.ToString("F6", CultureInfo.InvariantCulture)} " +
                                $"{(0.0).ToString("F6", CultureInfo.InvariantCulture)}");
                        }

                        // --- Step 3: Write faces using the position-based index map ---
                        foreach (var face in triangulation)
                        {
                            var verts = face.GetVertices().ToArray();
                            if (verts.Length != 3) continue; // skip degenerate

                            int idx1 = vertexIndices[(verts[0].Position.X, verts[0].Position.Y)];
                            int idx2 = vertexIndices[(verts[1].Position.X, verts[1].Position.Y)];
                            int idx3 = vertexIndices[(verts[2].Position.X, verts[2].Position.Y)];

                            writer.WriteLine($"f {idx1} {idx2} {idx3}");
                        }

                        // newline at end
                        writer.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting OBJ: {ex.Message}", "Export OBJ Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }




        private void ExportInsertionLog(string logFilePath)
        {
            lock (_lock)
            {
                using (var writer = new StreamWriter(logFilePath))
                {
                    writer.WriteLine("# Point Insertion Log");
                    writer.WriteLine("# Each line represents a vertex in the order it was inserted");
                    writer.WriteLine("# Format: Index X Y");

                    for (int i = 0; i < insertedVertices.Count; i++)
                    {
                        var v = insertedVertices[i];
                        writer.WriteLine($"{i + 1} {v.Position.X:F6} {v.Position.Y:F6}");
                    }
                }
            }
        }
    }
}
