using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp2.items
{
    // Menu builder (instance-based)
    public class MenuBuilder
    {
        private readonly ToolStripLabel _stepSpeedLabel;
        private readonly ToolStripComboBox _stepSpeedComboBox;
        private readonly Dictionary<string, float> _speedOptions;

        public MenuBuilder(ToolStripLabel stepSpeedLabel, ToolStripComboBox stepSpeedComboBox, Dictionary<string, float> speedOptions)
        {
            _stepSpeedLabel = stepSpeedLabel ?? throw new ArgumentNullException(nameof(stepSpeedLabel));
            _stepSpeedComboBox = stepSpeedComboBox ?? throw new ArgumentNullException(nameof(stepSpeedComboBox));
            _speedOptions = speedOptions ?? new Dictionary<string, float> { { "1x (Normal)", 1.0f } };
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
            Action<bool> setStepMode,
            Action<string> onSpeedSelected)
        {
            var menuStrip = new MenuStrip
            {
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.Professional
            };

            var fileMenu = new ToolStripMenuItem("File");
            var resetMenu = new ToolStripMenuItem("Reset Everything");
            resetMenu.Click += (s, e) => onReset();
            var exportObjMenu = new ToolStripMenuItem("Export Triangulation (.obj)");
            exportObjMenu.Click += (s, e) => onExport();
            fileMenu.DropDownItems.Add(resetMenu);
            fileMenu.DropDownItems.Add(exportObjMenu);

            var viewMenu = new ToolStripMenuItem("View");

            var showTriangles = new ToolStripMenuItem("Show Triangles") { Checked = getShowTriangles() };
            showTriangles.Click += (s, e) =>
            {
                var newVal = !getShowTriangles();
                setShowTriangles(newVal);
                showTriangles.Checked = newVal;
            };

            var showVoronoi = new ToolStripMenuItem("Show Voronoi") { Checked = getShowVoronoi() };
            showVoronoi.Click += (s, e) =>
            {
                var newVal = !getShowVoronoi();
                setShowVoronoi(newVal);
                showVoronoi.Checked = newVal;
            };

            var hover = new ToolStripMenuItem("Highlight Hovered Triangle") { Checked = getHoverHighlight() };
            hover.Click += (s, e) =>
            {
                var newVal = !getHoverHighlight();
                setHoverHighlight(newVal);
                hover.Checked = newVal;
            };

            var stepMode = new ToolStripMenuItem("Step-by-Step Mode") { Checked = getStepMode() };
            stepMode.Click += (s, e) =>
            {
                var newVal = !getStepMode();
                setStepMode(newVal);
                stepMode.Checked = newVal;
                _stepSpeedLabel.Visible = newVal;
                _stepSpeedComboBox.Visible = newVal;
            };

            viewMenu.DropDownItems.Add(showTriangles);
            viewMenu.DropDownItems.Add(showVoronoi);
            viewMenu.DropDownItems.Add(hover);
            viewMenu.DropDownItems.Add(stepMode);

            // Step controls
            _stepSpeedLabel.Font = new Font("Segoe UI", 13, FontStyle.Regular);
            _stepSpeedLabel.ForeColor = Color.DimGray;
            _stepSpeedLabel.Margin = new Padding(20, 0, 5, 0);
            _stepSpeedLabel.Visible = getStepMode();

            _stepSpeedComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _stepSpeedComboBox.Width = 120;
            _stepSpeedComboBox.Visible = getStepMode();

            foreach (var label in _speedOptions.Keys)
                _stepSpeedComboBox.Items.Add(label);

            _stepSpeedComboBox.SelectedItem = _speedOptions.Keys.First();
            _stepSpeedComboBox.SelectedIndexChanged += (s, e) =>
            {
                var selected = _stepSpeedComboBox.SelectedItem?.ToString() ?? "";
                onSpeedSelected(selected);
            };

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);
            menuStrip.Items.Add(_stepSpeedLabel);
            menuStrip.Items.Add(_stepSpeedComboBox);

            return menuStrip;
        }
    }

}
