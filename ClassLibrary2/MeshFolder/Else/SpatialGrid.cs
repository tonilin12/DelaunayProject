using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClassLibrary2.MeshFolder.Else
{
    /// <summary>
    /// SpatialGrid: purely for ordering points into a grid layout (row-major).
    /// No neighbor queries, just a sorted point array.
    /// </summary>
    public class SpatialGrid
    {
        public Vertex[] Points { get; private set; }   // Ordered points
        public int GridSize { get; private set; }      // Number of bins per row/column
        public Vector2 MinBounds { get; private set; }
        public Vector2 MaxBounds { get; private set; }

        private float width, height;
        private const float ClampFactor = 0.99f;
        private const float Epsilon = 1e-6f;

        public SpatialGrid(Vertex[] points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Length == 0) throw new ArgumentException("Point set cannot be empty");

            ConstructGrid(points);
        }

        private void ConstructGrid(Vertex[] arr)
        {
            // Compute bounds
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var v in arr)
            {
                min.X = Math.Min(min.X, v.Position.X);
                min.Y = Math.Min(min.Y, v.Position.Y);
                max.X = Math.Max(max.X, v.Position.X);
                max.Y = Math.Max(max.Y, v.Position.Y);
            }
            MinBounds = min;
            MaxBounds = max;

            width = Math.Max(max.X - min.X, Epsilon);
            height = Math.Max(max.Y - min.Y, Epsilon);

            // Compute grid size (approx sqrt of total points for square layout)
            GridSize = Math.Max(1, (int)Math.Ceiling(Math.Pow(arr.Length, 0.5)));
            float invWidth = ClampFactor * GridSize / width;
            float invHeight = ClampFactor * GridSize / height;

            // Sort points into bins
            Vertex[] sorted = new Vertex[arr.Length];
            int[] binCounts = new int[GridSize * GridSize];

            // Count points per bin
            int[] binIndex = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                int row = (int)((arr[i].Position.Y - min.Y) * invHeight);
                int col = (int)((arr[i].Position.X - min.X) * invWidth);
                row = Math.Clamp(row, 0, GridSize - 1);
                col = Math.Clamp(col, 0, GridSize - 1);
                int idx = row * GridSize + col;
                binIndex[i] = idx;
                binCounts[idx]++;
            }

            // Compute offsets
            int[] binOffsets = new int[GridSize * GridSize];
            int offset = 0;
            for (int i = 0; i < binCounts.Length; i++)
            {
                binOffsets[i] = offset;
                offset += binCounts[i];
                binCounts[i] = 0; // reuse as insertion counter
            }

            // Fill sorted array
            for (int i = 0; i < arr.Length; i++)
            {
                int idx = binIndex[i];
                int pos = binOffsets[idx] + binCounts[idx];
                sorted[pos] = arr[i];
                binCounts[idx]++;
            }

            Points = sorted; // final ordered array
        }
    }
}
