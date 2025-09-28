import numpy as np
from pathlib import Path
from scipy.spatial import KDTree
import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns

def read_obj_points(filename="points_fixed.obj") -> np.ndarray:
    """
    Reads a simple OBJ file containing only vertex positions (v x y z).
    Only x and y are returned as a Nx2 array.

    Args:
        filename (str): Name of the OBJ file (located in the same folder as this script).

    Returns:
        np.ndarray: Nx2 array of points (float32)
    """
    script_dir = Path(__file__).parent
    file_path = script_dir / filename

    if not file_path.is_file():
        raise FileNotFoundError(f"File not found: {file_path}")

    points = []
    with file_path.open("r") as f:
        for line in f:
            if line.startswith("v "):
                parts = line.strip().split()
                if len(parts) >= 3:
                    x, y = float(parts[1]), float(parts[2])
                    points.append([x, y])

    points_2d = np.array(points, dtype=np.float32)
    print(f"Loaded {len(points_2d)} points from {file_path}")
    print(f"First 5 points:\n{points_2d[:5]}")
    print(f"Total points count: {points_2d.shape[0]}")
    return points_2d


def check_poisson_disk(points: np.ndarray, min_dist: float):
    """
    Checks if a point set satisfies a minimum distance (Poisson-disk property).

    Args:
        points (np.ndarray): Nx2 array of points.
        min_dist (float): Minimum allowed distance between points.

    Returns:
        bool: True if all points satisfy the minimum distance.
    """
    tree = KDTree(points)
    distances, _ = tree.query(points, k=2)
    nearest = distances[:, 1]
    min_actual = nearest.min()
    print(f"Minimum distance between points: {min_actual:.4f}")
    print(f"Expected minimum distance: {min_dist}")
    is_poisson = min_actual >= min_dist
    print(f"Poisson-disk property satisfied? {is_poisson}")
    return is_poisson


def plot_all_points(points: np.ndarray, title="All Points"):
    """
    Plots all points using Pandas + Seaborn.
    """
    df = pd.DataFrame(points, columns=["x", "y"])
    plt.figure(figsize=(6, 6))
    sns.scatterplot(data=df, x="x", y="y", s=10, color="blue", alpha=0.6)
    plt.title(title)
    plt.xlabel("x")
    plt.ylabel("y")
    plt.axis("equal")
    plt.show()


def plot_blue_noise_spectrum(points: np.ndarray, width=512, height=512):
    """
    Plots the radial power spectrum of the point set to inspect blue-noise characteristics.
    """
    img = np.zeros((height, width))
    xs = (points[:, 0] * width).astype(int)
    ys = (points[:, 1] * height).astype(int)
    xs = np.clip(xs, 0, width-1)
    ys = np.clip(ys, 0, height-1)
    img[ys, xs] = 1

    ps = np.abs(np.fft.fftshift(np.fft.fft2(img)))**2
    plt.figure(figsize=(6, 6))
    plt.imshow(np.log1p(ps), cmap='gray')
    plt.title("Power Spectrum (log scale)")
    plt.axis("off")
    plt.show()


if __name__ == "__main__":
    # Load OBJ points
    points = read_obj_points("points_fixed.obj")

    # Check Poisson-disk property
    expected_min_dist = 1.0  # adjust to your generation parameter
    check_poisson_disk(points, min_dist=expected_min_dist)

    # Plot all points
    plot_all_points(points, title="All Poisson Disk Points")

    # Plot blue-noise spectrum
    plot_blue_noise_spectrum(points)
