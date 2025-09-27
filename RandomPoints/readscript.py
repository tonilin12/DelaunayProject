import struct
import numpy as np
from pathlib import Path

def read_2d_points(filename="points.bin"):
    """
    Reads a binary file containing 2D points with a 4-byte header.
    
    Returns:
        points (np.ndarray): Nx2 array of float32 points.
    """
    file_path = Path(__file__).parent / filename

    with open(file_path, "rb") as f:
        # Read number of points (4-byte unsigned int)
        num_points = struct.unpack("I", f.read(4))[0]

        # Read the points as float32
        points = np.fromfile(f, dtype=np.float32, count=num_points*2).reshape(num_points, 2)

    print(f"Loaded {num_points} points from {file_path}")
    print("First 5 points:")
    print(points[:5])
    
    return points

# Example usage
if __name__ == "__main__":
    read_2d_points("points.bin")
