using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

public static class RuntimeAnalyzer
{
    public static void Run()
    {
        int[] testSizes = { 100, 500, 1000, 5000, 10000, 20000, 50000, 100000 };
        int trialsPerSize = 3;
        string outputFile = "runtime_analysis.csv";

        File.WriteAllText(outputFile, "n,trial,method,runtime_ms,runtime_s,triangle_count\n");
        Console.WriteLine("Starting runtime analysis...");

        foreach (int n in testSizes)
        {
            for (int trial = 1; trial <= trialsPerSize; trial++)
            {
                Console.WriteLine($"\n=== Test: n={n}, trial={trial} ===");

                // Generate stable Vector2 points ONCE
                List<Vector2> pointsList = GenerateStablePointsFast(n);
                Console.WriteLine($"Generated {pointsList.Count} stable points");

                // ---------------------------
                // SpatialGrid case (separate Vertex[] + super-triangle)
                // ---------------------------
                var gridVertices = pointsList.Select(p => new Vertex(p)).ToArray();


                var sw = Stopwatch.StartNew();
                Face superTriangleGrid = TriangulationOperation.GetSuperTriangle(gridVertices);

                var grid = new SpatialGrid(gridVertices);
                var triangulatorGrid = new DelaunayBuilder(superTriangleGrid, grid.Points);

                triangulatorGrid.ProcessAllVertices();
                sw.Stop();

                int gridTriangleCount = triangulatorGrid.GetInternalTriangles().Count();
                Log(outputFile, n, trial, "SpatialGrid", sw.ElapsedMilliseconds, gridTriangleCount);

                // ---------------------------
                // Raw case (separate Vertex[] + super-triangle)
                // ---------------------------
                if (n <= 20000)
                {
                    var rawVertices = pointsList.Select(p => new Vertex(p)).ToArray();
                    sw.Restart();


                    Face superTriangleRaw = TriangulationOperation.GetSuperTriangle(rawVertices);

                    var triangulatorRaw = new DelaunayBuilder(superTriangleRaw, rawVertices);

                    triangulatorRaw.ProcessAllVertices();
                    sw.Stop();

                    int rawTriangleCount = triangulatorRaw.GetInternalTriangles().Count();
                    Log(outputFile, n, trial, "Raw", sw.ElapsedMilliseconds, rawTriangleCount);
                }
            }
        }

        Console.WriteLine($"\nRuntime analysis complete. Results saved to {outputFile}");
    }

    private static void Log(string file, int n, int trial, string method, long runtimeMs, int triangleCount)
    {
        double runtimeSec = runtimeMs / 1000.0;
        string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4:F3},{5}",
            n, trial, method, runtimeMs, runtimeSec, triangleCount);
        File.AppendAllText(file, line + Environment.NewLine);
        Console.WriteLine($"[n={n} | trial={trial}] {method}: {runtimeMs} ms ({runtimeSec:F3} s) | triangles={triangleCount}");
    }

    // Same as before, but returns Vector2s
    private static List<Vector2> GenerateStablePointsFast(int n)
    {
        const float minDist = 2f;
        const float maxCoord = 1000f;
        const int gridCells = 100;

        var list = new List<Vector2>(n);
        var rand = new Random(42);

        float cellSize = maxCoord / gridCells;
        var grid = new List<Vector2>[gridCells, gridCells];
        for (int i = 0; i < gridCells; i++)
            for (int j = 0; j < gridCells; j++)
                grid[i, j] = new List<Vector2>();

        while (list.Count < n)
        {
            float x = (float)rand.NextDouble() * maxCoord;
            float y = (float)rand.NextDouble() * maxCoord;
            var candidate = new Vector2(x, y);

            int gx = Math.Clamp((int)(x / cellSize), 0, gridCells - 1);
            int gy = Math.Clamp((int)(y / cellSize), 0, gridCells - 1);

            bool tooClose = false;
            for (int i = Math.Max(0, gx - 1); i <= Math.Min(gridCells - 1, gx + 1) && !tooClose; i++)
            {
                for (int j = Math.Max(0, gy - 1); j <= Math.Min(gridCells - 1, gy + 1) && !tooClose; j++)
                {
                    foreach (var v in grid[i, j])
                    {
                        if (Vector2.DistanceSquared(v, candidate) < minDist * minDist)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
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
