using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassLibrary2.MeshFolder.Else;

namespace WinFormsApp2.items
{
    public class TriangulationManager
    {
        public DelaunayBuilder? Builder { get; private set; }

        private bool inserting = false;

        public HalfEdge? ActiveEdge { get; private set; } = null;
        public Vertex? ActiveVertex { get; private set; } = null;

        /// <summary>
        /// FlipState values:
        ///   0 = no flip
        ///   1 = flip pending (circumcircle violation detected)
        ///   2 = flip completed (for one-frame highlight)
        /// </summary>
        public int FlipState { get; private set; } = 0;

        public bool IsInserting => inserting;

        public bool StepMode { get; set; } = false;

        // Fast-forward flag: when true, step mode runs without delay
        public bool FastForward { get; private set; } = false;

        private const int StepDelayMs = 1000;

        public event Action? VisualizationChanged;
        public event Action? ProcessingStateChanged;

        public void RequestFastForward()
        {
            // Immediately switch to "no delay" mode for the current step sequence
            FastForward = true;
        }

        public void InitializeFromScreen(Screen screen)
        {
            FastForward = true;

            Builder = null;
            ActiveEdge = null;
            ActiveVertex = null;
            FlipState = 0;

            int w = screen.Bounds.Width;
            int h = screen.Bounds.Height;

            var border = new[]
            {
                new Vertex(0, 0),
                new Vertex(w, 0),
                new Vertex(0, h),
                new Vertex(w, h)
            };

            var super = TriangulationOperation.GetSuperTriangle(border);
            Builder = new DelaunayBuilder(super);

            VisualizationChanged?.Invoke();
        }

        public (List<Face> faces, List<Vertex> verts) GetSnapshot()
        {
            var faces = Builder?.GetInternalTriangles().ToList() ?? new List<Face>();
            var verts = Builder?.GetInternalVertices()?.ToList() ?? new List<Vertex>();
            return (faces, verts);
        }

        public async Task AddVertexAsync(Vertex v)
        {
            if (Builder == null) return;
            if (inserting) return;

            inserting = true;
            ProcessingStateChanged?.Invoke();

            try
            {
                Builder.AddVertices(v);
                ActiveVertex = v;

                if (StepMode)
                {
                    await ProcessStepMode(v);
                }
                else
                {
                    await Task.Run(() => { Builder.ProcessSingleVertex(); });
                    VisualizationChanged?.Invoke();
                }
            }
            finally
            {
                inserting = false;
                FastForward = false;

                ActiveEdge = null;
                ActiveVertex = null;
                FlipState = 0;
            }
        }

        private async Task ProcessStepMode(Vertex v)
        {
            if (Builder == null)
                return;

            var enumerator = Builder.ProcessSingleVertexStepByStep().GetEnumerator();

            while (true)
            {
                if (!enumerator.MoveNext())
                    break;

                // If last step was pending, show "completed"
                if (FlipState == 1)
                {
                    FlipState = 2;
                    await StepModeTriggerRepaint();
                    FlipState = 0;
                }

                ActiveEdge = enumerator.Current;

                bool violation =
                    ActiveEdge != null &&
                    GeometryUtils.InCircumcircle(ActiveEdge.Face, v);

                FlipState = violation ? 1 : 0;

                await StepModeTriggerRepaint();

                if (FastForward)
                {
                    // Consume remaining steps without delay / visual stepping
                    while (enumerator.MoveNext()) { }
                    break;
                }
            }

            ActiveEdge = null;
            FlipState = 0;

            VisualizationChanged?.Invoke();
            
        }

        private async Task StepModeTriggerRepaint()
        {
            if (ActiveEdge == null && ActiveVertex == null)
                return;

            VisualizationChanged?.Invoke();

            bool doDelay =
                StepMode &&
                !FastForward &&
                StepDelayMs > 0;

            if (doDelay)
                await Task.Delay(StepDelayMs);
            else
                await Task.Yield();
        }
    }
}
