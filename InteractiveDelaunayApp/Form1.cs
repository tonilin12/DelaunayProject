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
        private readonly TriangulationManager tri_manager;
        private readonly MenuStrip menuStrip;

        private bool showTriangles = true;
        private bool showVoronoi = false;
        private bool showHoverHighlight = false;

        private Face? hoveredFace = null;

        private readonly MenuBuilder menuBuilder;
        private readonly Renderer renderer;
        private readonly TriangulationExporter exporter;

        public Form1()
        {
            InitializeComponent();

            Text = "Interactive Incremental Triangulation";
            DoubleBuffered = true;

            SizeChanged += Form1_Resize;

            var screen = Screen.FromControl(this);
            ClientSize = new Size(
                (int)(screen.Bounds.Width * 0.8f),
                (int)(screen.Bounds.Height * 0.8f));

            tri_manager = new TriangulationManager();
            tri_manager.VisualizationChanged += OnManagerVisualizationChanged;

            renderer = new Renderer();
            exporter = new TriangulationExporter();
            menuBuilder = new MenuBuilder();

            menuStrip = menuBuilder.BuildMenu(
                onReset: ResetEverything,
                onExport: ExportCurrent,
                onFastForward: tri_manager.RequestFastForward,
                getShowTriangles: () => showTriangles,
                setShowTriangles: v => { showTriangles = v; Invalidate(); },
                getShowVoronoi: () => showVoronoi,
                setShowVoronoi: v => { showVoronoi = v; Invalidate(); },
                getHoverHighlight: () => showHoverHighlight,
                setHoverHighlight: v => { showHoverHighlight = v; Invalidate(); },
                getStepMode: () => tri_manager.StepMode,
                setStepMode: v => tri_manager.StepMode = v
            );

            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            InitializeTriangulationFromScreen(screen);

            Paint += Form1_Paint;
            MouseClick += Form1_MouseClick;
            MouseMove += Form1_MouseMove;
        }

        private void InitializeTriangulationFromScreen(Screen s) =>
            tri_manager.InitializeFromScreen(s);

        private void OnManagerVisualizationChanged()
        {
            if (IsHandleCreated && !IsDisposed)
                BeginInvoke(new Action(Invalidate));
        }

        private async void Form1_MouseClick(object? sender, MouseEventArgs e)
        {
            if (tri_manager.IsInserting) return;
            if (menuStrip?.Bounds.Contains(e.Location) == true) return;

            int h = ClientSize.Height;

            // Use ScreenToWorld helper
            var world = Renderer.ScreenToWorld(new PointF(e.X, e.Y), h);
            var vertex = new Vertex(world);

            try
            {
                await tri_manager.AddVertexAsync(vertex);
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
            var snapshot = tri_manager.GetSnapshot();
            if (snapshot.faces == null || snapshot.faces.Count == 0) return;

            int h = ClientSize.Height;

            // Use ScreenToWorld helper
            var worldP = Renderer.ScreenToWorld(new PointF(e.X, e.Y), h);

            bool IsPointInFace(Face face, Vertex q)
            {
                var (destinationEdge, _) = MeshNavigator.LocatePointInMesh(face, q);
                return destinationEdge?.Face == face;
            }

            var newlyHovered = snapshot.faces
                .FirstOrDefault(face => IsPointInFace(face, new Vertex(worldP)));

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

            // Use WorldToScreen helper for the circumcenter
            var centerScreen = Renderer.WorldToScreen(center, formHeight);

            float x = centerScreen.X - r - 2f;
            float y = centerScreen.Y - r - 2f;
            float diameter = 2f * r;

            return new RectangleF(x, y, diameter + 4f, diameter + 4f);
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            if (tri_manager == null) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int h = ClientSize.Height;
            var snapshot = tri_manager.GetSnapshot();

            if (showTriangles)
                renderer.DrawTriangles(g, snapshot.faces, h);

            renderer.DrawVertices(g, snapshot.verts, h);

            if (showVoronoi && tri_manager.Builder != null)
                renderer.DrawVoronoi(g, tri_manager.Builder, h);

            if (showTriangles && showHoverHighlight && hoveredFace != null)
                renderer.DrawHoverFace(g, hoveredFace, h);

            // Flip visualization using new naming (ActiveEdge, ActiveVertex, FlipState)
            if (showTriangles && tri_manager.ActiveEdge != null && tri_manager.ActiveVertex != null)
            {
                // highlight all edges incident to the active vertex
                renderer.DrawEdgesForVertex(g, tri_manager.ActiveVertex, h, Color.SeaGreen, 3f);

                if (tri_manager.FlipState == 2)
                {
                    // flip just finished: emphasize the flipped edge
                    renderer.DrawHalfEdge(g, tri_manager.ActiveEdge!, h, Color.SkyBlue, 4f);
                }
                else
                {
                    // 1 = pending flip (Delaunay violation), 0 = normal
                    bool flipPending = (tri_manager.FlipState == 1);
                    renderer.DrawEdgeFace(g, tri_manager.ActiveEdge!, h, flipPending);
                }
            }
        }

        private void ResetEverything()
        {
            tri_manager.InitializeFromScreen(Screen.FromControl(this));
            hoveredFace = null;
            Invalidate();
        }

        private void ExportCurrent()
        {
            var snap = tri_manager.GetSnapshot();
            if (snap.faces == null || snap.faces.Count == 0 || snap.verts.Count == 0)
            {
                MessageBox.Show(
                    "No triangulation data to export.",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            string projectDir = AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string exportDir = Path.Combine(projectDir, "Exports", $"Triangulation_{timestamp}");

            try
            {
                exporter.Export(snap.faces, snap.verts, exportDir);
                MessageBox.Show(
                    $"Triangulation export finished!\n{exportDir}",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to export: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void Form1_Resize(object? sender, EventArgs e) => Invalidate();
        private void Form1_Load(object? sender, EventArgs e) { }
    }
}
