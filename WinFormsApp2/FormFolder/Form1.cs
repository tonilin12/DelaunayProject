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
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp2.FormFolder;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        #region Fields

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

        private Face hoveredFace = null!;
        private bool hasError = false;

        private bool showTriangles = true;
        private bool showVoronoi = false;

        
        private const int VertexProcessTimeoutMs = 1000;

        private bool isProcessingVertex = false;

        private bool isStepByStepMode = false; // default: step-by-step animation



        // --- Step-by-step animation speed control ---
        private int baseDelayMs = 400; // default base speed (1x = 250 ms)
        private float speedMultiplier = 1.0f; // 1x speed by default
        private int CurrentDelayMs => (int)(baseDelayMs * speedMultiplier);

        // UI controls
        private ToolStrip? stepControlToolStrip;
        private ToolStripLabel? stepSpeedLabel;
        private ToolStripComboBox? stepSpeedComboBox;


        private Face? flippingFace = null; // only for drawing circumcircle during step animation


        private bool showHoverHighlight = false; // default: off


        #endregion

        #region Constructor

        public Form1()
        {
            this.Text = "Interactive Incremental Triangulation";
            this.DoubleBuffered = true;

            // ---- Set initial form size relative to the screen containing this form ----
            var currentScreen = Screen.FromControl(this);
            int screenWidth = currentScreen.Bounds.Width;
            int screenHeight = currentScreen.Bounds.Height;

            // For example, 80% of the screen size
            this.ClientSize = new Size(
                (int)(screenWidth * 0.8f),
                (int)(screenHeight * 0.8f)
            );

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


        #endregion

        #region Menu Initialization

        private void InitializeMenu()
        {
            // ---------------- Menu Strip ----------------
            menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 14, FontStyle.Regular)
            };

            // ---------------- File Menu ----------------
            fileMenu = new ToolStripMenuItem("File");

            resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += ResetMenu_Click;

            exportObjMenu = new ToolStripMenuItem("Export Triangulation (.obj)");
            exportObjMenu.Click += ExportObjMenu_Click;

            fileMenu.DropDownItems.Add(resetMenu);
            fileMenu.DropDownItems.Add(exportObjMenu);

            // ---------------- View Menu ----------------
            viewMenu = new ToolStripMenuItem("View");

            // Show triangles toggle
            showTrianglesMenuItem = new ToolStripMenuItem("Show Triangles")
            {
                Checked = showTriangles
            };
            showTrianglesMenuItem.Click += (s, e) =>
            {
                showTriangles = !showTriangles;
                showTrianglesMenuItem.Checked = showTriangles;
                Invalidate();
            };

            // Show Voronoi toggle
            showVoronoiMenuItem = new ToolStripMenuItem("Show Voronoi")
            {
                Checked = showVoronoi
            };
            showVoronoiMenuItem.Click += (s, e) =>
            {
                showVoronoi = !showVoronoi;
                showVoronoiMenuItem.Checked = showVoronoi;
                Invalidate();
            };

            // Hover highlight toggle (default off)
            var hoverHighlightMenuItem = new ToolStripMenuItem("Highlight Hovered Triangle")
            {
                Checked = showHoverHighlight
            };
            hoverHighlightMenuItem.Click += (s, e) =>
            {
                showHoverHighlight = !showHoverHighlight;
                hoverHighlightMenuItem.Checked = showHoverHighlight;
                Invalidate();
            };

            // Step-by-step mode toggle
            var stepModeMenuItem = new ToolStripMenuItem("Step-by-Step Mode")
            {
                Checked = isStepByStepMode
            };
            stepModeMenuItem.Click += (s, e) =>
            {
                isStepByStepMode = !isStepByStepMode;
                stepModeMenuItem.Checked = isStepByStepMode;
                if (stepControlToolStrip != null)
                    stepControlToolStrip.Visible = isStepByStepMode;
            };

            // Add menu items to View
            viewMenu.DropDownItems.Add(showTrianglesMenuItem);
            viewMenu.DropDownItems.Add(showVoronoiMenuItem);
            viewMenu.DropDownItems.Add(hoverHighlightMenuItem);
            viewMenu.DropDownItems.Add(stepModeMenuItem);

            // ---------------- Step Speed Control ----------------
            stepControlToolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top,
                Padding = new Padding(4, 2, 4, 2),
                Visible = isStepByStepMode
            };

            stepSpeedLabel = new ToolStripLabel("Speed:");
            stepSpeedComboBox = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 80
            };

            var speedOptions = new Dictionary<string, float>
            {
                { "0.5x (Faster)", 0.5f },
                { "1x (Normal)", 1.0f },
                { "2x (Slower)", 2.0f },
                { "3x (Much Slower)", 3.0f },
                { "4x (Very Slow)", 4.0f }
            };

            foreach (var label in speedOptions.Keys)
                stepSpeedComboBox.Items.Add(label);

            stepSpeedComboBox.SelectedItem = "1x (Normal)";
            stepSpeedComboBox.SelectedIndexChanged += (s, e) =>
            {
                var selectedLabel = stepSpeedComboBox.SelectedItem?.ToString();
                if (selectedLabel != null && speedOptions.TryGetValue(selectedLabel, out var mult))
                {
                    lock (_lock)
                    {
                        speedMultiplier = mult;
                    }
                }
            };

            stepControlToolStrip.Items.Add(stepSpeedLabel);
            stepControlToolStrip.Items.Add(stepSpeedComboBox);

            // ---------------- Add menus to form ----------------
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            this.Controls.Add(stepControlToolStrip);
        }



        #endregion

        #region TriangInitialization

        private void InitializeTriangulation()
        {
            hasError = false;

            lock (_lock)
            {
                triangulation?.Clear();
                triangulation = null;
                triangulator = null;
                hoveredFace = null!;
                superTriangle = null!;
                insertedVertices.Clear();

                var currentScreen = Screen.FromControl(this);
                int screenWidth = currentScreen.Bounds.Width;
                int screenHeight = currentScreen.Bounds.Height;

                var borderPoints = new Vertex[]
                {
                    new Vertex(0, 0),
                    new Vertex(screenWidth, 0),
                    new Vertex(0, screenHeight),
                    new Vertex(screenWidth, screenHeight)
                };

                try
                {
                    TriangulationOperation.GetSuperTriangle(borderPoints, out superTriangle);
                    triangulator = new TriangulationBuilder(superTriangle);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    MessageBox.Show($"Critical error initializing triangulation: {ex.Message}",
                                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        #endregion

        #region Reset

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
        

                // 2️⃣ Fully break reference graphs
                triangulation?.Clear();
                triangulation = null;

                insertedVertices.Clear();
                insertedVertices = new List<Vertex>();

                hoveredFace = null!;
                superTriangle = null!;
                triangulator = null;
                hasError = false;


                // 4️⃣ Reinitialize new state
                InitializeTriangulation();
                Invalidate();
            }
        }

        #endregion

        #region Vertex Processing

        private HalfEdge? FindNextExpectedFlipEdge(Vertex vertex)
        {
            foreach (var edge in vertex.GetVertexEdges().Reverse())
            {
                var twinNext = edge.Next?.Twin;
                if (twinNext != null && GeometryUtils.IsInsideOrOnCircumcircle(twinNext.Face, vertex))
                    return twinNext;

            }

            return null;
        }

        private async Task ProcessVertexStepByStepAsync(Vertex vertex)
        {
            if (triangulator == null || isProcessingVertex) return;

            isProcessingVertex = true;
            try
            {
                triangulator.AddVertices(vertex);

                if (isStepByStepMode)
                {
                    var enumerator = triangulator.ProcessSingleVertexStepByStep().GetEnumerator();
                    bool hasNext;

                    do
                    {
                        HalfEdge? edgeToFlip;
                        lock (_lock)
                        {
                            hasNext = enumerator.MoveNext();

                            // Update main triangulation
                            triangulation = triangulator.GetInternalTriangles().ToHashSet();

                            // Determine current face to highlight
                            edgeToFlip = FindNextExpectedFlipEdge(vertex);
                            flippingFace = edgeToFlip?.Face;

                            // Add vertex to list if not already
                            if (!insertedVertices.Contains(vertex))
                                insertedVertices.Add(vertex);
                        }

                        // Full redraw
                        this.BeginInvoke(() => Invalidate());

                        // Short delay before overlay
                        await Task.Delay(CurrentDelayMs / 3);

                        // Overlay circle is drawn automatically via Paint
                        // so we just call Invalidate again to ensure it's painted
                        if (flippingFace != null)
                            this.BeginInvoke(() => Invalidate());

                        // Step delay
                        if (hasNext)
                            await Task.Delay(CurrentDelayMs);

                    } while (hasNext);

                    // Clear overlay after finishing
                    flippingFace = null;
                    this.BeginInvoke(() => Invalidate());
                }
                else
                {
                    // Instant mode
                    await Task.Run(() =>
                    {
                        lock (_lock)
                        {
                            triangulator.ProcessSingleVertex();
                            insertedVertices.Add(vertex);
                            triangulation = triangulator.GetInternalTriangles().ToHashSet();
                        }
                    });

                    this.BeginInvoke(() => Invalidate());
                }
            }
            finally
            {
                isProcessingVertex = false;
            }
        }


        #endregion

        #region Mouse Events

        private async void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (hasError || isProcessingVertex) return; // prevent click during animation
            if (menuStrip.Bounds.Contains(e.Location)) return;

            var vertex = new Vertex(e.X, e.Y);

            try
            {
                await ProcessVertexStepByStepAsync(vertex);
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
            Face newHovered = null!;

            lock (_lock)
            {
                newHovered = triangulation.FirstOrDefault(face => GeometryUtils.IsPointInsideTriangle(face, p));
            }

            if (newHovered != hoveredFace)
            {
                // Compute old & new hover regions
                RectangleF oldRegion = hoveredFace != null ? GetHoverRegion(hoveredFace) : RectangleF.Empty;
                RectangleF newRegion = newHovered != null ? GetHoverRegion(newHovered) : RectangleF.Empty;

                hoveredFace = newHovered;

                // Invalidate only the changed areas
                if (!oldRegion.IsEmpty) Invalidate(Rectangle.Ceiling(oldRegion));
                if (!newRegion.IsEmpty) Invalidate(Rectangle.Ceiling(newRegion));
            }
        }

        #endregion

        #region Paint

        private RectangleF GetHoverRegion(Face face)
        {
            if (face == null) return RectangleF.Empty;

            Vector2 center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);
            float diameter = r * 2f;

            return new RectangleF(center.X - r - 2, center.Y - r - 2, diameter + 4, diameter + 4);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (hasError) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            HashSet<Face> trianglesSnapshot;
            List<Vertex> verticesSnapshot;
            Face hoveredSnapshot;

            lock (_lock)
            {
                trianglesSnapshot = triangulation != null ? new HashSet<Face>(triangulation) : new HashSet<Face>();
                verticesSnapshot = new List<Vertex>(insertedVertices);
                hoveredSnapshot = hoveredFace;
            }

            // ---- Draw main elements ----
            if (showTriangles)
                DrawTriangles(g, trianglesSnapshot);

            DrawVertices(g, verticesSnapshot);

            if (showVoronoi && triangulator != null)
                DrawVoronoi(g, triangulator);

            // ---- Hover overlay ----
            if (showTriangles && hoveredSnapshot != null && showHoverHighlight)
                DrawHoveredTriangle(g, hoveredSnapshot);


            if (flippingFace != null)
            {
                DrawHoveredTriangle(g,flippingFace); // reuse hover circle drawing
            }
        }

        #endregion

        #region Drawing Helpers

        private void DrawTriangles(Graphics g, HashSet<Face> triangles)
        {
            using (var edgePen = new Pen(Color.Blue, 2))
            {
                foreach (var face in triangles)
                {
                    var verts = face.GetVertices().Select(v => new PointF(v.Position.X, v.Position.Y)).ToArray();
                    if (verts.Length < 3) break;

                    for (int i = 0; i < verts.Length; i++)
                        g.DrawLine(edgePen, verts[i], verts[(i + 1) % verts.Length]);
                }
            }
        }

        private void DrawVertices(Graphics g, List<Vertex> vertices)
        {
            using (var pointBrush = new SolidBrush(Color.Red))
            {
                float radius = 3;
                foreach (var v in vertices)
                    g.FillEllipse(pointBrush, v.Position.X - radius, v.Position.Y - radius, radius * 2, radius * 2);
            }
        }
        private void DrawHoveredTriangle(Graphics g, Face face)
        {
            var verts = face.GetVertices().Select(v => new PointF(v.Position.X, v.Position.Y)).ToArray();
            if (verts.Length < 3) return;

            // ---- Draw triangle edges in black ----
            using (var pen = new Pen(Color.Black, 3)) // black edges
            {
                for (int i = 0; i < verts.Length; i++)
                    g.DrawLine(pen, verts[i], verts[(i + 1) % verts.Length]);
            }

            // ---- Draw circumcircle ----
            Vector2 center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);
            if (r > 0f)
            {
                using (var circlePen = new Pen(Color.Green, 2)) // bright green circumcircle
                {
                    g.DrawEllipse(circlePen, center.X - r, center.Y - r, r * 2, r * 2);
                }
            }
        }





        private void DrawVoronoi(Graphics g, TriangulationBuilder triangulator)
        {
            using (var voronoiPen = new Pen(Color.DarkOrange, 2))
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
                        g.DrawLine(voronoiPen, p1.X, p1.Y, p2.X, p2.Y);
                    }
                }
            }
        }

        #endregion

        #region Export OBJ + Insertion Log

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

                    string projectDir = AppDomain.CurrentDomain.BaseDirectory;
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string exportDir = Path.Combine(projectDir, "Exports", $"Triangulation_{timestamp}");
                    Directory.CreateDirectory(exportDir);

                    string objFilePath = Path.Combine(exportDir, "triangulation.obj");
                    string logFilePath = Path.Combine(exportDir, "insertion_log.txt");

                    ExportTriangulationToObj(objFilePath);
                    ExportInsertionLog(logFilePath);

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

                        var allVerts = triangulation.SelectMany(f => f.GetVertices()).ToList();

                        var vertexList = allVerts
                            .GroupBy(v => (v.Position.X, v.Position.Y))
                            .Select(g => g.First())
                            .OrderBy(v => v.Position.X)
                            .ThenBy(v => v.Position.Y)
                            .ToList();

                        var vertexIndices = new Dictionary<(float x, float y), int>();
                        for (int i = 0; i < vertexList.Count; i++)
                        {
                            var v = vertexList[i];
                            var key = (v.Position.X, v.Position.Y);
                            vertexIndices[key] = i + 1;

                            writer.WriteLine(
                                $"v {v.Position.X.ToString("F6", CultureInfo.InvariantCulture)} " +
                                $"{v.Position.Y.ToString("F6", CultureInfo.InvariantCulture)} " +
                                $"{(0.0).ToString("F6", CultureInfo.InvariantCulture)}");
                        }

                        foreach (var face in triangulation)
                        {
                            var verts = face.GetVertices().ToArray();
                            if (verts.Length != 3) continue;

                            int idx1 = vertexIndices[(verts[0].Position.X, verts[0].Position.Y)];
                            int idx2 = vertexIndices[(verts[1].Position.X, verts[1].Position.Y)];
                            int idx3 = vertexIndices[(verts[2].Position.X, verts[2].Position.Y)];

                            writer.WriteLine($"f {idx1} {idx2} {idx3}");
                        }

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

        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }
    }
}
