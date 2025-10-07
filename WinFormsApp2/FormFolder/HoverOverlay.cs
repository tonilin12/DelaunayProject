using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.ComponentModel;

namespace WinFormsApp2.FormFolder
{
    public class HoverOverlay : Control
    {
        private Face hoveredFace;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Face HoveredFace
        {
            get => hoveredFace;
            set
            {
                if (hoveredFace != value)
                {
                    hoveredFace = value;
                    Invalidate(); // automatically repaint when hover changes
                }
            }
        }

        public HoverOverlay()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (HoveredFace == null)
                return;

            using (var pen = new Pen(Color.LimeGreen, 1))
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Vector2 center = HoveredFace.Circumcenter;
                var vertices = HoveredFace.GetVertices().ToArray();
                if (vertices.Length < 1) return;

                float r = Vector2.Distance(center, vertices[0].Position);

                g.DrawEllipse(pen, center.X - r, center.Y - r, r * 2, r * 2);
            }
        }
    }
}
