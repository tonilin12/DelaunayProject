using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsApp2.items
{
    // Menu builder (instance-based) - with auto FastForward integration
    public class MenuBuilder
    {
        private readonly TriangulationManager _manager;

        public MenuBuilder(TriangulationManager manager)
        {
            _manager = manager;
        }

        public MenuStrip BuildMenu(
            Action onReset,
            Action onExport,
            Func<bool> getShowTriangles,
            Action<bool> setShowTriangles,
            Func<bool> getShowVoronoi,
            Action<bool> setShowVoronoi,
            Func<bool> getHoverHighlight,
            Action<bool> setHoverHighlight,
            Func<bool> getStepMode,
            Action<bool> setStepMode)
        {
            var menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.Professional
            };

            // --- File Menu ---
            var fileMenu = new ToolStripMenuItem("File");

            var resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += (s, e) =>
            {
                // Immediately stop any ongoing delayed animation
                _manager.FastForward();
                onReset();
            };

            var exportObjMenu = new ToolStripMenuItem("Export Triangulation (.obj)");
            exportObjMenu.Click += (s, e) => onExport();

            fileMenu.DropDownItems.Add(resetMenu);
            fileMenu.DropDownItems.Add(exportObjMenu);

            // --- View Menu ---
            var viewMenu = new ToolStripMenuItem("View");

            // Show Triangles
            var showTriangles = new ToolStripMenuItem("Show Triangles")
            {
                Checked = getShowTriangles()
            };

            // Highlight Hovered Triangle
            var hover = new ToolStripMenuItem("Highlight Hovered Triangle")
            {
                Checked = getHoverHighlight(),
                Visible = getShowTriangles()
            };

            // Step-by-Step Mode
            var stepMode = new ToolStripMenuItem("Step-by-Step Mode")
            {
                Checked = getStepMode(),
                Visible = getShowTriangles()
            };

            // Show Voronoi
            var showVoronoi = new ToolStripMenuItem("Show Voronoi")
            {
                Checked = getShowVoronoi()
            };

            // --- Helper to update the top-level "View" menu badge ---
            Action UpdateViewBadge = () =>
            {
                var parts = new List<string>();
                if (getShowTriangles()) parts.Add("T");
                if (getShowVoronoi()) parts.Add("V");
                if (getHoverHighlight()) parts.Add("H");
                if (getStepMode()) parts.Add("S");

                string badge = parts.Count > 0 ? $" [{string.Join(" ", parts)}]" : "";
                viewMenu.Text = "View" + badge;

                viewMenu.ToolTipText = parts.Count > 0
                    ? "Enabled: " + string.Join(", ", parts.Select(p =>
                        p == "T" ? "Triangles" :
                        p == "V" ? "Voronoi" :
                        p == "H" ? "Hover highlight" :
                        p == "S" ? "Step mode" : p))
                    : "No view overlays enabled";
            };

            // --- Event Handlers ---

            showTriangles.Click += (s, e) =>
            {
                var newVal = !getShowTriangles();

                // If triangles are being turned off mid-animation, cancel immediately
                if (!newVal)
                    _manager.FastForward();

                setShowTriangles(newVal);
                showTriangles.Checked = newVal;

                if (!newVal)
                {
                    // Hide dependent features
                    setStepMode(false);
                    stepMode.Checked = false;
                    stepMode.Visible = false;

                    setHoverHighlight(false);
                    hover.Checked = false;
                    hover.Visible = false;
                }
                else
                {
                    // Restore dependent features
                    stepMode.Visible = true;
                    hover.Visible = true;
                }

                UpdateViewBadge();
            };

            showVoronoi.Click += (s, e) =>
            {
                var newVal = !getShowVoronoi();
                setShowVoronoi(newVal);
                showVoronoi.Checked = newVal;
                UpdateViewBadge();
            };

            hover.Click += (s, e) =>
            {
                var newVal = !getHoverHighlight();
                setHoverHighlight(newVal);
                hover.Checked = newVal;
                UpdateViewBadge();
            };

            stepMode.Click += (s, e) =>
            {
                var newVal = !getStepMode();
                setStepMode(newVal);
                stepMode.Checked = newVal;
                UpdateViewBadge();
            };

            // --- Build View menu ---
            viewMenu.DropDownItems.Add(showTriangles);
            viewMenu.DropDownItems.Add(showVoronoi);
            viewMenu.DropDownItems.Add(hover);
            viewMenu.DropDownItems.Add(stepMode);

            // Initial badge
            UpdateViewBadge();

            // Add menus to strip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);

            return menuStrip;
        }
    }
}
