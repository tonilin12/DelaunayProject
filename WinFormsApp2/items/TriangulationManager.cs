using ClassLibrary2.GeometryFolder;
using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Drawing;
using System.Threading.Tasks;
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
        public Vertex? CurrentVertex { get; private set; } = null;

        public TriangulationBuilder? Triangulator
        {
            get { lock (_lock) { return triangulator; } }
        }

        public event Action? StateChanged;
        public event Action? ProcessingStarted;
        public event Action? ProcessingFinished;

        // --- Step-by-step control ---
        private const int StepDelayMs = 1000; // normal delay per step
        public bool IsStepByStepMode { get; set; } = false;

        // --- New: immediate (fast-forward) flag ---
        private volatile bool immediateMode = false;
        public bool ImmediateMode
        {
            get { lock (_lock) { return immediateMode; } }
            set { lock (_lock) { immediateMode = value; } }
        }

        public void FastForward() => ImmediateMode = true;

        public bool flip_happen = false;

        // ---------------------
        // --- Initialization ---
        // ---------------------
        public void InitializeFromScreen(Screen screen)
        {
            // Fast-forward any ongoing step mode
            ImmediateMode = true;

            lock (_lock)
            {
                triangulation?.Clear();
                triangulation = null;
                triangulator = null;
                CurrentFace = null;
                CurrentVertex = null;

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

        // ---------------------
        // --- Snapshot helper ---
        // ---------------------
        public (HashSet<Face> triangles, List<Vertex> inserted) GetSnapshot()
        {
            lock (_lock)
            {
                var triCopy = triangulation != null ? new HashSet<Face>(triangulation) : new HashSet<Face>();
                var vertCopy = triangulator?.GetInternalVertices()?.ToList() ?? new List<Vertex>();
                return (triCopy, vertCopy);
            }
        }

        // ---------------------
        // --- Vertex Insertion ---
        // ---------------------
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
                CurrentVertex = vertex;

                if (IsStepByStepMode)
                {
                    var enumerator = triangulator.ProcessSingleVertexStepByStep().GetEnumerator();

                    while (true)
                    {
                        HalfEdge? edgeToFlip = null;

                        lock (_lock)
                        {
                            if (!enumerator.MoveNext())
                                break; // no more steps

                            triangulation = triangulator.GetInternalTriangles().ToHashSet();
                            edgeToFlip = enumerator.Current;
                            CurrentFace = edgeToFlip?.Face;

                            flip_happen = edgeToFlip != null &&
                                          GeometryUtils.IsInsideOrOnCircumcircle(CurrentFace!, vertex);
                        }

                        // --- Trigger repaint ---
                        if (CurrentFace != null || CurrentVertex != null)
                        {
                            StateChanged?.Invoke();

                            // --- Adaptive delay: skip if immediate mode ---
                            int delay;
                            lock (_lock)
                                delay = (IsStepByStepMode && !ImmediateMode) ? StepDelayMs : 0;

                            if (delay > 0)
                                await Task.Delay(delay);
                            else
                                await Task.Yield(); // yield to UI thread quickly
                        }

                        // --- If immediate mode is activated mid-way, fast-finish ---
                        if (ImmediateMode)
                        {
                            // drain remaining steps fast
                            while (enumerator.MoveNext()) { }
                            break;
                        }
                    }

                    // Clear temporary highlight after finishing
                    lock (_lock)
                    {
                        CurrentFace = null;
                        flip_happen = false;
                        CurrentVertex = null;
                    }

                    StateChanged?.Invoke();
                }
                else
                {
                    // Normal instant triangulation mode
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
                lock (_lock)
                {
                    isProcessingVertex = false;
                    // reset fast-forward after completion
                    ImmediateMode = false;
                }
                ProcessingFinished?.Invoke();
            }
        }
    }
}
