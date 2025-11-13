using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsApp2.items
{
    // Menu builder (instance-based) - Form injects callbacks; no external manager needed
    public class MenuBuilder
    {
        /// <summary>
        /// Builds a MenuStrip with File and View menus, wiring up all actions and toggles.
        /// </summary>
        public MenuStrip BuildMenu(
            Action onReset,
            Action onExport,
            Action onFastForward,
            Func<bool> getShowTriangles,
            Action<bool> setShowTriangles,
            Func<bool> getShowVoronoi,
            Action<bool> setShowVoronoi,
            Func<bool> getHoverHighlight,
            Action<bool> setHoverHighlight,
            Func<bool> getStepMode,
            Action<bool> setStepMode)
        {
            // Create the main MenuStrip
            var menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.Professional
            };

            // --- File Menu ---
            var fileMenu = new ToolStripMenuItem("File");

            // Reset Everything menu item
            var resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += (s, e) =>
            {
                // Stop any ongoing animations and reset the state
                onFastForward?.Invoke();
                onReset?.Invoke();
            };

            // Export menu item
            var exportObjMenu = new ToolStripMenuItem("Export Triangulation (.obj)");
            exportObjMenu.Click += (s, e) => onExport?.Invoke();

            // Add File menu items
            fileMenu.DropDownItems.Add(resetMenu);
            fileMenu.DropDownItems.Add(exportObjMenu);

            // --- View Menu ---
            var viewMenu = new ToolStripMenuItem("View");

            // Show Triangles toggle
            var showTriangles = new ToolStripMenuItem("Show Triangles")
            {
                Checked = getShowTriangles()
            };

            // Highlight Hovered Triangle toggle, visible only if triangles are shown
            var hover = new ToolStripMenuItem("Highlight Hovered Triangle")
            {
                Checked = getHoverHighlight(),
                Visible = getShowTriangles()
            };

            // Step-by-Step Triangulation Mode toggle, visible only if triangles are shown
            var stepMode = new ToolStripMenuItem("Step-by-Step Triangulation Mode")
            {
                Checked = getStepMode(),
                Visible = getShowTriangles()
            };

            // Show Voronoi toggle
            var showVoronoi = new ToolStripMenuItem("Show Voronoi")
            {
                Checked = getShowVoronoi()
            };

            // --- Badge updater ---
            // Updates the View menu text and tooltip to reflect currently active overlays
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

            // --- Click handlers ---
            showTriangles.Click += (s, e) =>
            {
                var newVal = !getShowTriangles();

                // If triangles are being turned off mid-animation, cancel immediately
                if (!newVal)
                    onFastForward?.Invoke();

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
                    // Make dependent features visible again
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

            // --- Assemble View menu ---
            viewMenu.DropDownItems.Add(showTriangles);
            viewMenu.DropDownItems.Add(showVoronoi);
            viewMenu.DropDownItems.Add(hover);
            viewMenu.DropDownItems.Add(stepMode);

            // Initialize badge
            UpdateViewBadge();

            // Add menus to the main MenuStrip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);

            return menuStrip;
        }
    }
}
