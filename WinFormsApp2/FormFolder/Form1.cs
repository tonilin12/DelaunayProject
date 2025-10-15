using ClassLibrary2.GeometryFolder;
using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using System.Globalization;
using System.Numerics;
using WinFormsApp2.items;
using System.Drawing;
using System.Windows.Forms;

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
        private Face? stack_face = null;

        // Step controls passed into builder
        private ToolStripLabel stepSpeedLabel = new ToolStripLabel();
        private ToolStripComboBox stepSpeedComboBox = new ToolStripComboBox();
        private Dictionary<string, float> speedOptions = new Dictionary<string, float>
        {
            { "(Normal)", 1.0f },
            { "(Slow)", 3.0f }
        };

        // instance helpers
        private MenuBuilder menuBuilder;
        private Renderer renderer;
        private TriangulationExporter exporter;

        public Form1()
        {
            InitializeComponent(); // designer or empty

            this.SizeChanged += Form1_Resize;


            this.Text = "Interactive Incremental Triangulation";
            this.DoubleBuffered = true;

            var screen = Screen.FromControl(this);
            this.ClientSize = new Size((int)(screen.Bounds.Width * 0.8f), (int)(screen.Bounds.Height * 0.8f));

            manager = new TriangulationManager();
            manager.StateChanged += OnManagerStateChanged;


            renderer = new Renderer();
            exporter = new TriangulationExporter();

            menuBuilder = new MenuBuilder(manager);

            // when building the menu, remove onSpeedSelected and related logic:
            menuStrip = menuBuilder.BuildMenu(
                onReset: ResetEverything,
                onExport: ExportCurrent,
                getShowTriangles: () => showTriangles,
                setShowTriangles: val => { showTriangles = val; Invalidate(); },
                getShowVoronoi: () => showVoronoi,
                setShowVoronoi: val => { showVoronoi = val; Invalidate(); },
                getHoverHighlight: () => showHoverHighlight,
                setHoverHighlight: val => { showHoverHighlight = val; Invalidate(); },
                getStepMode: () => manager.IsStepByStepMode,
                setStepMode: val => manager.IsStepByStepMode = val
            );

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            InitializeTriangulationFromScreen(screen);

            this.Paint += Form1_Paint;
            this.MouseClick += Form1_MouseClick;
            this.MouseMove += Form1_MouseMove;
        }

        private void InitializeTriangulationFromScreen(Screen s)
        {
            manager.InitializeFromScreen(s);
        }

        private void OnManagerStateChanged()
        {
            // manager events may be raised on a worker thread; marshal to UI thread
            if (this.IsHandleCreated && !this.IsDisposed)
                this.BeginInvoke(new Action(() =>
                {
                    stack_face = manager.CurrentFace;
                    Invalidate();
                }));
        }

        private async void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (manager.IsProcessingVertex) return;
            if (menuStrip.Bounds.Contains(e.Location)) return;

            int formHeight = this.ClientSize.Height;
            var vertex = new Vertex(e.X, formHeight - e.Y);

            try
            {
                await manager.AddVertexAsync(vertex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical triangulation error: {ex.Message}", "Triangulation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            var snapshot = manager.GetSnapshot();
            if (snapshot.triangles == null || snapshot.triangles.Count == 0) return;

            int formHeight = this.ClientSize.Height;
            var p = new Vector2(e.X, formHeight - e.Y);

            Face? newHovered = null;
            // snapshot.triangles is a local copy; no need to lock external object here.
            newHovered = snapshot.triangles.FirstOrDefault(face => GeometryUtils.IsPointInsideTriangle(face, p));

            if (newHovered != hoveredFace)
            {
                RectangleF oldRegion = hoveredFace != null ? GetHoverRegion(hoveredFace, formHeight) : RectangleF.Empty;
                RectangleF newRegion = newHovered != null ? GetHoverRegion(newHovered, formHeight) : RectangleF.Empty;
                hoveredFace = newHovered;
                if (!oldRegion.IsEmpty) Invalidate(Rectangle.Ceiling(oldRegion));
                if (!newRegion.IsEmpty) Invalidate(Rectangle.Ceiling(newRegion));
            }
        }

        private RectangleF GetHoverRegion(Face face, int formHeight)
        {
            if (face == null) return RectangleF.Empty;
            Vector2 center = face.Circumcenter;
            float r = Vector2.Distance(center, face.GetVertices().First().Position);
            float x = center.X - r - 2;
            float y = formHeight - (center.Y + r) - 2;
            float diameter = r * 2f;
            return new RectangleF(x, y, diameter + 4, diameter + 4);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (manager == null) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int formHeight = this.ClientSize.Height;

            var snapshot = manager.GetSnapshot();

            if (showTriangles)
                renderer.DrawTriangles(g, snapshot.triangles, formHeight);

            renderer.DrawVertices(g, snapshot.inserted, formHeight);

            if (showVoronoi && manager.Triangulator != null)
                renderer.DrawVoronoi(g, manager.Triangulator!, formHeight);

            if (showTriangles && hoveredFace != null && showHoverHighlight)
                renderer.DrawHoveredTriangle(g, hoveredFace, formHeight);


            var currentVertex = manager.CurrentVertex;
 

            if (showTriangles&& stack_face!=null && currentVertex != null)
            {



                renderer.DrawEdgesForVertex(g, currentVertex, formHeight, Color.SeaGreen, 3f);

                // If a flip happened, draw circumcircle in red; otherwise normal green
                Color circleColor = manager.flip_happen ? Color.Red : Color.Green;
                renderer.DrawHoveredTriangle(g, stack_face, formHeight, circleColor);

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
                MessageBox.Show("No triangulation data to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string projectDir = AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string exportDir = Path.Combine(projectDir, "Exports", $"Triangulation_{timestamp}");
            try
            {
                exporter.Export(snap.triangles, snap.inserted, exportDir);
                MessageBox.Show($"Triangulation export finished!\n{exportDir}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            // Optionally, you can update your triangulation or view bounds here
            // For now, just redraw everything
            Invalidate(); // triggers Form1_Paint
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
