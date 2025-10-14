using ClassLibrary2.GeometryFolder;
using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp2.items
{
    public class TriangulationManager
    {
        private readonly object _lock = new object();
        private HashSet<Face>? triangulation;
        private Face superTriangle = null!;
        private TriangulationBuilder? triangulator;

        private bool isProcessingVertex = false;
        public bool IsProcessingVertex
        {
            get { lock (_lock) { return isProcessingVertex; } }
        }

        public Face? CurrentFace { get; private set; } = null;

        public TriangulationBuilder? Triangulator
        {
            get { lock (_lock) { return triangulator; } }
        }

        public event Action? StateChanged;
        public event Action? ProcessingStarted;
        public event Action? ProcessingFinished;

        // Step-by-step control
        private int baseDelayMs = 500;
        private float speedMultiplier = 1.0f;
        public int CurrentDelayMs => (int)(baseDelayMs * speedMultiplier);
        public bool IsStepByStepMode { get; set; } = false;

        public bool flip_happen = false;

        public List<HalfEdge> CurrentEdgesToHighlight { get; private set; } = new List<HalfEdge>();


        public void SetSpeedMultiplier(float m)
        {
            lock (_lock) speedMultiplier = m;
        }

        public void InitializeFromScreen(Screen screen)
        {
            lock (_lock)
            {
                triangulation?.Clear();
                triangulation = null;
                triangulator = null;
                CurrentFace = null;

                int screenWidth = screen.Bounds.Width;
                int screenHeight = screen.Bounds.Height;

                var borderPoints = new Vertex[]
                {
                    new Vertex(0, 0),
                    new Vertex(screenWidth, 0),
                    new Vertex(0, screenHeight),
                    new Vertex(screenWidth, screenHeight)
                };

                TriangulationOperation.GetSuperTriangle(borderPoints, out superTriangle);
                triangulator = new TriangulationBuilder(superTriangle);
            }

            StateChanged?.Invoke();
        }

        public (HashSet<Face> triangles, List<Vertex> inserted) GetSnapshot()
        {
            lock (_lock)
            {
                var triCopy = triangulation != null ? new HashSet<Face>(triangulation) : new HashSet<Face>();
                var vertCopy = triangulator?.GetInternalVertices()?.ToList() ?? new List<Vertex>();

                return (triCopy, vertCopy);
            }
        }


        public async Task AddVertexAsync(Vertex vertex)
        {
            if (triangulator == null) return;

            lock (_lock)
            {
                if (isProcessingVertex) return;
                isProcessingVertex = true;
            }

            ProcessingStarted?.Invoke();

            try
            {
                triangulator.AddVertices(vertex);

                if (IsStepByStepMode)
                {
                    var enumerator = triangulator.ProcessSingleVertexStepByStep().GetEnumerator();

                    while (true)
                    {
                        HalfEdge? edgeToFlip = null;

                        lock (_lock)
                        {
                            if (!enumerator.MoveNext())
                                break; // Exit loop when no more steps

                            triangulation = triangulator.GetInternalTriangles().ToHashSet();
                            edgeToFlip = enumerator.Current;
                            CurrentFace = edgeToFlip?.Face;

                            flip_happen = edgeToFlip != null &&
                                          GeometryUtils.IsInsideOrOnCircumcircle(CurrentFace!, vertex);

                            // Highlight edges connected to this vertex
                            CurrentEdgesToHighlight = vertex.GetVertexEdges()
                                                            .Select(e => e.Next!)
                                                            .Where(e => e != null)
                                                            .ToList();
                        }

                        // Trigger UI repaint
                        if (CurrentFace != null || CurrentEdgesToHighlight.Count > 0)
                        {
                            StateChanged?.Invoke();
                            await Task.Delay(CurrentDelayMs);
                        }
                    }

                    // Clear temporary highlight after finishing
                    lock (_lock)
                    {
                        CurrentFace = null;
                        CurrentEdgesToHighlight.Clear();
                        flip_happen = false;
                    }

                    StateChanged?.Invoke();
                }
                else
                {
                    await Task.Run(() =>
                    {
                        lock (_lock)
                        {
                            triangulator.ProcessSingleVertex();
                            triangulation = triangulator.GetInternalTriangles().ToHashSet();
                        }
                    });

                    StateChanged?.Invoke();
                }
            }
            finally
            {
                lock (_lock) isProcessingVertex = false;
                ProcessingFinished?.Invoke();
            }
        }

    }




}
