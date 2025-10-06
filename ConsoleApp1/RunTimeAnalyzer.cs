using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;

public static class RuntimeAnalyzer
{
    public static void Run()
    {
        int[] testSizes = { 100, 500, 1000, 5000, 10000, 50000, 100000 };
        int trialsPerSize = 3;
        string outputFile = "runtime_analysis.csv";

        // CSV header
        File.WriteAllText(outputFile, "n,trial,method,runtime_ms,runtime_s,num_points\n");

        Console.WriteLine("Starting runtime analysis...");

        foreach (int n in testSizes)
        {
            for (int trial = 1; trial <= trialsPerSize; trial++)
            {
                Console.WriteLine($"\n=== Test: n={n}, trial={trial} ===");

                // Generate stable vertices efficiently
                List<Vertex> verticesList = GenerateStableVerticesFast(n);
                Console.WriteLine($"Generated {verticesList.Count} stable vertices");

                // Convert list to array once
                Vertex[] verticesArray = verticesList.ToArray();

                // Create supertriangle
                Vertex[] borderPoints = new Vertex[]
                {
                    new Vertex(0, 0),
                    new Vertex(1000, 0),
                    new Vertex(0, 1000)
                };
                Face superTriangle;
                TriangulationOperation.getSuperTriangle(ref borderPoints, out superTriangle);

                // ---------------------------
                // SpatialGrid version (always tested)
                // ---------------------------
                var sw = Stopwatch.StartNew();
                var grid = new SpatialGrid(verticesArray);        // Pass array directly
                Vertex[] gridPoints = grid.Points;  // Direct access to internal array

                var triangulatorGrid = new TriangulationBuilder(superTriangle, gridPoints);
                triangulatorGrid.ProcessAllVertices();
                sw.Stop();
                Log(outputFile, n, trial, "SpatialGrid", sw.ElapsedMilliseconds, verticesArray.Length);

                // ---------------------------
                // Raw array version (only for n <= 10k)
                // ---------------------------
                if (n <= 10000)
                {
                    sw.Restart();
                    var triangulatorRaw = new TriangulationBuilder(superTriangle, verticesArray);
                    triangulatorRaw.ProcessAllVertices();
                    sw.Stop();
                    Log(outputFile, n, trial, "Raw", sw.ElapsedMilliseconds, verticesArray.Length);
                }
            }
        }

        Console.WriteLine($"\nRuntime analysis complete. Results saved to {outputFile}");
    }

    private static void Log(string file, int n, int trial, string method, long runtimeMs, int numPoints)
    {
        double runtimeSec = runtimeMs / 1000.0;

        // Use period for decimal separator to ensure compatibility
        string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4:F0},{5},{6}",
            n, trial, method, runtimeMs, runtimeSec, numPoints, DateTime.Now);

        File.AppendAllText(file, line + Environment.NewLine);

        Console.WriteLine($"[n={n} | trial={trial}] {method}: {runtimeMs} ms ({runtimeSec:F3} s) for {numPoints} points");
    }


    /// <summary>
    /// Fast stable vertex generator using a coarse grid to avoid O(n^2) checks.
    /// Ensures minimum distance between points.
    /// </summary>
    private static List<Vertex> GenerateStableVerticesFast(int n)
    {
        const float minDist = 2f;
        const float maxCoord = 1000f;
        const int gridCells = 100; // coarse grid for distance checking

        var list = new List<Vertex>(n);
        var rand = new Random(42);

        float cellSize = maxCoord / gridCells;
        var grid = new List<Vertex>[gridCells, gridCells];
        for (int i = 0; i < gridCells; i++)
            for (int j = 0; j < gridCells; j++)
                grid[i, j] = new List<Vertex>();

        while (list.Count < n)
        {
            float x = (float)rand.NextDouble() * maxCoord;
            float y = (float)rand.NextDouble() * maxCoord;
            var candidate = new Vertex(x, y);

            int gx = Math.Clamp((int)(x / cellSize), 0, gridCells - 1);
            int gy = Math.Clamp((int)(y / cellSize), 0, gridCells - 1);

            bool tooClose = false;

            // Check neighboring 9 cells
            for (int i = Math.Max(0, gx - 1); i <= Math.Min(gridCells - 1, gx + 1); i++)
            {
                for (int j = Math.Max(0, gy - 1); j <= Math.Min(gridCells - 1, gy + 1); j++)
                {
                    foreach (var v in grid[i, j])
                    {
                        if (Vector2.DistanceSquared(v.Position, candidate.Position) < minDist * minDist)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) break;
                }
                if (tooClose) break;
            }

            if (!tooClose)
            {
                list.Add(candidate);
                grid[gx, gy].Add(candidate);
            }
        }

        return list;
    }
}
