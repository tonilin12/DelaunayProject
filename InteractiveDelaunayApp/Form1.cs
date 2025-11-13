using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ClassLibrary2.MeshFolder.Else;
using WinFormsApp2.items;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private TriangulationManager manager;
        private MenuStrip menuStrip;

        private bool showTriangles = true;
        private bool showVoronoi = false;
        private bool showHoverHighlight = false;

        private Face? hoveredFace = null;

        private MenuBuilder menuBuilder;
        private Renderer renderer;
        private TriangulationExporter exporter;

        public Form1()
        {
            InitializeComponent();

            Text = "Interactive Incremental Triangulation";
            DoubleBuffered = true;

            SizeChanged += Form1_Resize;

            var screen = Screen.FromControl(this);
            ClientSize = new Size((int)(screen.Bounds.Width * 0.8f), (int)(screen.Bounds.Height * 0.8f));

            manager = new TriangulationManager();
            manager.StateChanged += OnManagerStateChanged;

            renderer = new Renderer();
            exporter = new TriangulationExporter();

            menuBuilder = new MenuBuilder();

            menuStrip = menuBuilder.BuildMenu(
                onReset: ResetEverything,
                onExport: ExportCurrent,
                onFastForward: manager.FastForward,
                getShowTriangles: () => showTriangles,
                setShowTriangles: v => { showTriangles = v; Invalidate(); },
                getShowVoronoi: () => showVoronoi,
                setShowVoronoi: v => { showVoronoi = v; Invalidate(); },
                getHoverHighlight: () => showHoverHighlight,
                setHoverHighlight: v => { showHoverHighlight = v; Invalidate(); },
                getStepMode: () => manager.IsStepByStepMode,
                setStepMode: v => manager.IsStepByStepMode = v
            );

            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            InitializeTriangulationFromScreen(screen);

            Paint += Form1_Paint;
            MouseClick += Form1_MouseClick;
            MouseMove += Form1_MouseMove;
        }

        private void InitializeTriangulationFromScreen(Screen s) => manager.InitializeFromScreen(s);

        private void OnManagerStateChanged()
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(Invalidate));
        }

        private async void Form1_MouseClick(object? sender, MouseEventArgs e)
        {
            if (manager.IsProcessingVertex) return;
            if (menuStrip?.Bounds.Contains(e.Location) == true) return;

            int h = ClientSize.Height;
            var vertex = new Vertex(e.X, h - e.Y);

            try
            {
                await manager.AddVertexAsync(vertex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Critical triangulation error: {ex.Message}",
                    "Triangulation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void Form1_MouseMove(object? sender, MouseEventArgs e)
        {
            var snapshot = manager.GetSnapshot();
            if (snapshot.triangles == null || snapshot.triangles.Count == 0) return;

            int h = ClientSize.Height;
            var p = new Vector2(e.X, h - e.Y);

            bool IsPointInFace(Face face, Vertex q)
            {
                var (destinationEdge, _) = MeshNavigator.LocatePointInMesh(face, q);
                return destinationEdge?.Face == face;
            }

            var newlyHovered = snapshot.triangles.FirstOrDefault(face => IsPointInFace(face, new Vertex(p)));

            if (!ReferenceEquals(newlyHovered, hoveredFace))
            {
                var oldRegion = hoveredFace != null ? GetHoverRegion(hoveredFace, h) : RectangleF.Empty;
                var newRegion = newlyHovered != null ? GetHoverRegion(newlyHovered, h) : RectangleF.Empty;

                hoveredFace = newlyHovered;

                if (!oldRegion.IsEmpty) Invalidate(Rectangle.Ceiling(oldRegion));
                if (!newRegion.IsEmpty) Invalidate(Rectangle.Ceiling(newRegion));
            }
        }

        private static RectangleF GetHoverRegion(Face? face, int formHeight)
        {
            if (face == null) return RectangleF.Empty;

            var center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);

            float x = center.X - r - 2f;
            float y = formHeight - (center.Y + r) - 2f;
            float diameter = 2f * r;

            return new RectangleF(x, y, diameter + 4f, diameter + 4f);
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            if (manager == null) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int h = ClientSize.Height;
            var snapshot = manager.GetSnapshot();

            if (showTriangles)
                renderer.DrawTriangles(g, snapshot.triangles, h);

            renderer.DrawVertices(g, snapshot.inserted, h);

            if (showVoronoi && manager.Triangulator != null)
                renderer.DrawVoronoi(g, manager.Triangulator!, h);

            if (showTriangles && showHoverHighlight && hoveredFace != null)
                renderer.DrawHoverFace(g, hoveredFace, h);

            // Flip visualization with original flags
            if (showTriangles && manager.CurrentEdge != null && manager.CurrentVertex != null)
            {
                renderer.DrawEdgesForVertex(g, manager.CurrentVertex, h, Color.SeaGreen, 3f);

                if (manager.flip_finished)
                {
                    renderer.DrawHalfEdge(g, manager.CurrentEdge!, h, Color.SkyBlue, 4f);
                }
                else
                {
                    renderer.DrawEdgeFace(g, manager.CurrentEdge!, h, manager.flip_happen);
                }
            }
        }

        private void ResetEverything()
        {
            manager.InitializeFromScreen(Screen.FromControl(this));
            hoveredFace = null;
            Invalidate();
        }

        private void ExportCurrent()
        {
            var snap = manager.GetSnapshot();
            if (snap.triangles == null || snap.triangles.Count == 0 || snap.inserted.Count == 0)
            {
                MessageBox.Show("No triangulation data to export.", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDir = AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string exportDir = Path.Combine(projectDir, "Exports", $"Triangulation_{timestamp}");

            try
            {
                exporter.Export(snap.triangles, snap.inserted, exportDir);
                MessageBox.Show($"Triangulation export finished!\n{exportDir}", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Resize(object? sender, EventArgs e) => Invalidate();

        private void Form1_Load(object? sender, EventArgs e) { }
    }
}
