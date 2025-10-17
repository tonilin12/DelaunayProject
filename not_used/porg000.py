import numpy as np
import matplotlib.pyplot as plt
from scipy.spatial import Delaunay
from matplotlib.animation import FuncAnimation

# --- Renderer-style color palette ---
COLOR_TRIANGLE = "#4169E1"   # RoyalBlue
COLOR_VERTEX = "#191970"     # MidnightBlue
COLOR_PENDING = "#8FBC8F"    # DarkSeaGreen
COLOR_LABEL = "#FFFFFF"      # White text labels

# --- Points ---
points = np.array([
    [343.91705,165.94649], [560.07577,553.96912], [811.67035,141.1414],
    [560,191], [560,355], [449,279], [688,277],
    [663,199], [635,174], [470,344], [652,357], [560,235],
])

fig, ax = plt.subplots(figsize=(9,6))
ax.set_aspect('equal')

def draw_tris(ax, pts):
    """Draw Delaunay triangulation for pts (if >=3)."""
    if len(pts) < 3:
        return
    tri = Delaunay(pts)
    for simplex in tri.simplices:
        tri_pts = pts[simplex]
        xs = [tri_pts[0,0], tri_pts[1,0], tri_pts[2,0], tri_pts[0,0]]
        ys = [tri_pts[0,1], tri_pts[1,1], tri_pts[2,1], tri_pts[0,1]]
        ax.plot(xs, ys, '-', color=COLOR_TRIANGLE, linewidth=1.6, alpha=0.9)

def update(frame):
    ax.clear()
    ax.set_aspect('equal')
    ax.set_facecolor("#F8F9FA")  # subtle light gray background (like WinForms default)
    ax.tick_params(left=False, bottom=False, labelleft=False, labelbottom=False)

    k = frame // 2
    show_tri = (frame % 2) == 1
    k = min(k, len(points)-1)
    prev_pts = points[:k]
    current_pts = points[:k+1]

    if not show_tri:
        draw_tris(ax, prev_pts)

        if len(prev_pts) > 0:
            ax.scatter(prev_pts[:,0], prev_pts[:,1], color=COLOR_VERTEX, s=40, zorder=3, edgecolors="white", linewidths=0.8)
            for i, p in enumerate(prev_pts):
                ax.text(p[0]+2, p[1]+2, str(i), color=COLOR_LABEL, fontsize=9, weight='bold')

        newp = points[k]
        ax.scatter(newp[0], newp[1], color=COLOR_PENDING, s=120, marker='o', edgecolors="white", linewidths=1.2, zorder=5)
        ax.text(newp[0]+2, newp[1]+2, str(k), color=COLOR_LABEL, fontsize=9, weight='bold')

    else:
        draw_tris(ax, current_pts)
        ax.scatter(current_pts[:,0], current_pts[:,1], color=COLOR_VERTEX, s=40, zorder=3, edgecolors="white", linewidths=0.8)
        for i, p in enumerate(current_pts):
            ax.text(p[0]+2, p[1]+2, str(i), color=COLOR_LABEL, fontsize=9, weight='bold')

    ax.set_title(
        f"Insert step {k} — {'Triangulation' if show_tri else 'Pending Point'}",
        color=COLOR_VERTEX, fontsize=12, weight='bold'
    )
    ax.relim()
    ax.autoscale_view()

# --- Animation setup ---
total_frames = len(points) * 2
ani = FuncAnimation(fig, update, frames=total_frames, interval=800, repeat=False)

plt.show()
