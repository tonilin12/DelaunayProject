import numpy as np
from pathlib import Path
from scipy.spatial import KDTree
import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns

def read_obj_points_2d(filename: str = "points_fixed.obj") -> np.ndarray:
    """
    Reads 2D points from an OBJ file (x, y only).
    Returns points as (N,2) array.
    """
    script_dir = Path(__file__).parent if "__file__" in globals() else Path.cwd()
    file_path = script_dir / filename

    if not file_path.is_file():
        raise FileNotFoundError(f"File not found: {file_path}")

    points = []
    with file_path.open("r") as f:
        for line in f:
            if line.startswith("v "):
                parts = line.strip().split()
                if len(parts) >= 3:  # x, y
                    x, y = float(parts[1]), float(parts[2])
                    points.append([x, y])

    points_arr = np.array(points, dtype=np.float64)
    print(f"Loaded {len(points_arr)} points from {file_path}")
    if len(points_arr) > 0:
        print(f"First 5 points:\n{points_arr[:5]}")
    return points_arr

def evaluate_pointcloud_2d(points: np.ndarray, cv_threshold: float = 0.6):
    """
    Evaluates 2D point cloud quality for triangulation or mesh operations.
    Returns metrics and a suitability assessment.
    """
    n_points = points.shape[0]
    metrics = {}
    print(f"\n--- 2D Point Cloud Quality Metrics for {n_points} points ---")

    if n_points < 2:
        return {
            "metrics": {},
            "assessment": "Too few points to evaluate quality."
        }

    # --- Nearest-neighbor distances ---
    tree = KDTree(points)
    distances, _ = tree.query(points, k=2)
    nearest = distances[:, 1]

    min_dist = nearest.min()
    mean_dist = nearest.mean()
    std_dist = nearest.std()
    cv_dist = std_dist / mean_dist if mean_dist != 0 else float('inf')
    uniform_pass = cv_dist <= cv_threshold

    metrics.update({
        "min_distance": min_dist,
        "mean_distance": mean_dist,
        "std_distance": std_dist,
        "cv_distance": cv_dist,
        "uniform_pass": uniform_pass
    })

    # --- Coverage (voxel/grid-based) ---
    bbox_min = points.min(axis=0)
    bbox_max = points.max(axis=0)
    bbox_size = bbox_max - bbox_min
    grid_size = mean_dist
    grid_indices = np.floor((points - bbox_min) / grid_size).astype(int)
    unique_cells = np.unique(grid_indices, axis=0)
    coverage_ratio = len(unique_cells) / np.prod(np.ceil(bbox_size / grid_size))
    metrics["coverage_ratio"] = coverage_ratio

    # --- Outlier detection (points far from neighbors) ---
    z_score = (nearest - mean_dist) / std_dist
    outlier_mask = np.abs(z_score) > 3
    outlier_ratio = np.sum(outlier_mask) / n_points
    metrics["outlier_ratio"] = outlier_ratio

    # --- Generate assessment ---
    assessment = []
    if n_points < 10:
        assessment.append("Too few points for reliable triangulation.")
    if not uniform_pass:
        assessment.append("Points are unevenly spaced; triangulation may produce degenerate triangles.")
    if coverage_ratio < 0.8:
        assessment.append("Point cloud has poor coverage; some regions may be under-sampled.")
    if outlier_ratio > 0.05:
        assessment.append("Significant outliers detected; consider cleaning the point cloud.")

    if not assessment:
        assessment.append("Point cloud appears suitable for triangulation and mesh operations.")

    print("\n--- Assessment ---")
    for line in assessment:
        print("-", line)

    return {
        "metrics": metrics,
        "assessment": assessment
    }

def plot_pointcloud_2d(points: np.ndarray, title="2D Point Cloud", show=True):
    """
    Plots 2D point cloud using matplotlib.
    """
    df = pd.DataFrame(points, columns=["x", "y"])
    plt.figure(figsize=(6, 6))
    sns.scatterplot(data=df, x="x", y="y", s=10, alpha=0.6)
    plt.title(title)
    plt.xlabel("X")
    plt.ylabel("Y")
    plt.axis("equal")
    if show:
        plt.show()

if __name__ == "__main__":
    OBJ_FILENAME = "points_fixed.obj"
    CV_THRESHOLD = 0.6

    # --- Read points ---
    pts = read_obj_points_2d(OBJ_FILENAME)

    # --- Evaluate quality ---
    results = evaluate_pointcloud_2d(pts, cv_threshold=CV_THRESHOLD)

    # --- Plot ---
    plot_pointcloud_2d(pts, title="2D Point Cloud Quality Check")

    # --- Optional: print metrics ---
    print("\nMetrics:")
    for k, v in results["metrics"].items():
        print(f"{k}: {v:.4f}")
