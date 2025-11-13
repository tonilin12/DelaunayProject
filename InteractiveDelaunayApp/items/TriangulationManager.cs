using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassLibrary2.MeshFolder.Else;

namespace WinFormsApp2.items
{
    public class TriangulationManager
    {
        private readonly object _lock = new object();
        private DelaunayBuilder? triangulator;

        private bool isProcessingVertex = false;
        public bool IsProcessingVertex
        {
            get { lock (_lock) { return isProcessingVertex; } }
        }

        public HalfEdge? CurrentEdge { get; private set; } = null;
        public Vertex? CurrentVertex { get; private set; } = null;

        public bool flip_happen = false;
        public bool flip_finished = false;

        public DelaunayBuilder? Triangulator
        {
            get { lock (_lock) { return triangulator; } }
        }

        public event Action? StateChanged;

        /// <summary>
        /// Unified processing state event.
        /// Fired both when processing starts and when it ends.
        /// Subscribers must check IsProcessingVertex to know state.
        /// </summary>
        public event Action? ProcessingStateChanged;

        // --- Step-by-step control ---
        private const int StepDelayMs = 1000;
        public bool IsStepByStepMode { get; set; } = false;

        // --- Immediate (fast-forward) ---
        private volatile bool immediateMode = false;
        public bool ImmediateMode
        {
            get { lock (_lock) { return immediateMode; } }
            set { lock (_lock) { immediateMode = value; } }
        }

        public void FastForward() => ImmediateMode = true;

        // ---------------------
        // --- Initialization ---
        // ---------------------
        public void InitializeFromScreen(Screen screen)
        {
            ImmediateMode = true;

            lock (_lock)
            {
                triangulator = null;
                CurrentEdge = null;
                CurrentVertex = null;

                int w = screen.Bounds.Width;
                int h = screen.Bounds.Height;

                var borderPoints = new Vertex[]
                {
                    new Vertex(0, 0),
                    new Vertex(w, 0),
                    new Vertex(0, h),
                    new Vertex(w, h)
                };

                var superTriangle = TriangulationOperation.GetSuperTriangle(borderPoints);
                triangulator = new DelaunayBuilder(superTriangle);
            }

            StateChanged?.Invoke();
        }

        // ---------------------
        // --- Snapshot helper ---
        // ---------------------
        public (List<Face> triangles, List<Vertex> inserted) GetSnapshot()
        {
            lock (_lock)
            {
                var triCopy = triangulator?.GetInternalTriangles().ToList() ?? new List<Face>();
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

            // notify "processing started"
            ProcessingStateChanged?.Invoke();

            try
            {
                triangulator.AddVertices(vertex);
                CurrentVertex = vertex;

                if (IsStepByStepMode)
                {
                    var enumerator = triangulator
                        .ProcessSingleVertexStepByStep()
                        .GetEnumerator();

                    while (true)
                    {
                        if (!enumerator.MoveNext())
                            break;

                        if (flip_happen)
                        {
                            flip_finished = true;
                            await TryTriggerRepaintAsync();
                            flip_finished = false;
                        }

                        CurrentEdge = enumerator.Current;

                        flip_happen = CurrentEdge != null &&
                                      GeometryUtils.InCircumcircle(CurrentEdge.Face, vertex);

                        await TryTriggerRepaintAsync();

                        if (ImmediateMode)
                        {
                            while (enumerator.MoveNext()) { }
                            break;
                        }
                    }

                    lock (_lock)
                    {
                        CurrentEdge = null;
                        flip_happen = false;
                        CurrentVertex = null;
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
                    ImmediateMode = false;
                }

                // notify "processing finished"
                ProcessingStateChanged?.Invoke();
            }
        }

        private async Task TryTriggerRepaintAsync()
        {
            if (CurrentEdge == null && CurrentVertex == null)
                return;

            StateChanged?.Invoke();

            int delay = (IsStepByStepMode && !ImmediateMode) ? StepDelayMs : 0;

            if (delay > 0)
                await Task.Delay(delay);
            else
                await Task.Yield();
        }
    }
}
