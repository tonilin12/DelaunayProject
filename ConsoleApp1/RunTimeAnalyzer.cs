using ClassLibrary2.MeshFolder.Else;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime;

public static class RuntimeAnalyzer
{
    public static void Run()
    {
        int[] testSizes = { 100, 500, 1000, 5000, 10000, 20000, 50000, 100000 };
        int trialsPerSize = 3;
        string outputFile = "runtime_analysis.csv";

        File.WriteAllText(outputFile, "n,trial,method,runtime_ms,runtime_s,triangle_count,memory_mb,memory_bytes,gc_impact_percent\n");
        Console.WriteLine("Starting runtime analysis...");

        // Force GC before starting for a clean baseline
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (int n in testSizes)
        {
            for (int trial = 1; trial <= trialsPerSize; trial++)
            {
                Console.WriteLine($"\n=== Test: n={n}, trial={trial} ===");

                long baselineMemory = GC.GetTotalMemory(true);

                List<Vector2> pointsList = GenerateStablePointsFast(n);
                Console.WriteLine($"Generated {pointsList.Count} stable points");

                // ---------------------------
                // SpatialGrid case
                // ---------------------------
                var gridVertices = new Vertex[pointsList.Count];
                for (int i = 0; i < pointsList.Count; i++)
                    gridVertices[i] = new Vertex(pointsList[i]);

                long preTriMemory = GC.GetTotalMemory(false);

                int gc0Before = GC.CollectionCount(0);
                int gc1Before = GC.CollectionCount(1);
                int gc2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();

                Face superTriangleGrid = TriangulationOperation.GetSuperTriangle(gridVertices);
                var grid = new SpatialGrid(gridVertices);
                var triangulatorGrid = new DelaunayBuilder(superTriangleGrid, grid.Points);
                triangulatorGrid.ProcessAllVertices();

                sw.Stop();

                int gc0After = GC.CollectionCount(0);
                int gc1After = GC.CollectionCount(1);
                int gc2After = GC.CollectionCount(2);

                long postTriMemory = GC.GetTotalMemory(false);

                int deltaGc0 = gc0After - gc0Before;
                int deltaGc1 = gc1After - gc1Before;
                int deltaGc2 = gc2After - gc2Before;

                // Rough weighted GC impact (ms)
                double gcImpactMs = deltaGc0 * 0.1 + deltaGc1 * 1.0 + deltaGc2 * 10.0;
                double gcImpactPercent = (gcImpactMs / sw.Elapsed.TotalMilliseconds) * 100.0;

                int gridTriangleCount = triangulatorGrid.GetInternalTriangles().Count();
                long gridMemoryUsed = postTriMemory - preTriMemory;

                Log(outputFile, n, trial, "SpatialGrid", sw.ElapsedMilliseconds,
                    gridTriangleCount, gridMemoryUsed, gcImpactPercent);

                // Cleanup
                triangulatorGrid = null;
                grid = null;
                superTriangleGrid = null;
                gridVertices = null;

                // ---------------------------
                // Raw case (up to 20k only to save memory)
                // ---------------------------
                if (n <= 20000)
                {
                    var rawVertices = new Vertex[pointsList.Count];
                    for (int i = 0; i < pointsList.Count; i++)
                        rawVertices[i] = new Vertex(pointsList[i]);

                    preTriMemory = GC.GetTotalMemory(false);

                    gc0Before = GC.CollectionCount(0);
                    gc1Before = GC.CollectionCount(1);
                    gc2Before = GC.CollectionCount(2);

                    sw.Restart();

                    Face superTriangleRaw = TriangulationOperation.GetSuperTriangle(rawVertices);
                    var triangulatorRaw = new DelaunayBuilder(superTriangleRaw, rawVertices);
                    triangulatorRaw.ProcessAllVertices();

                    sw.Stop();

                    gc0After = GC.CollectionCount(0);
                    gc1After = GC.CollectionCount(1);
                    gc2After = GC.CollectionCount(2);

                    postTriMemory = GC.GetTotalMemory(false);

                    deltaGc0 = gc0After - gc0Before;
                    deltaGc1 = gc1After - gc1Before;
                    deltaGc2 = gc2After - gc2Before;

                    gcImpactMs = deltaGc0 * 0.1 + deltaGc1 * 1.0 + deltaGc2 * 10.0;
                    gcImpactPercent = (gcImpactMs / sw.Elapsed.TotalMilliseconds) * 100.0;

                    int rawTriangleCount = triangulatorRaw.GetInternalTriangles().Count();
                    long rawMemoryUsed = postTriMemory - preTriMemory;

                    Log(outputFile, n, trial, "Raw", sw.ElapsedMilliseconds,
                        rawTriangleCount, rawMemoryUsed, gcImpactPercent);

                    // Cleanup
                    triangulatorRaw = null;
                    superTriangleRaw = null;
                    rawVertices = null;
                }

                // Force cleanup between trials
                pointsList.Clear();
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        Console.WriteLine($"\nRuntime analysis complete. Results saved to {outputFile}");
    }

    private static void Log(string file, int n, int trial, string method, long runtimeMs,
                            int triangleCount, long memoryBytes, double gcImpactPercent)
    {
        double runtimeSec = runtimeMs / 1000.0;
        double memoryMB = memoryBytes / (1024.0 * 1024.0);

        string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4:F3},{5},{6:F2},{7},{8:F2}",
            n, trial, method, runtimeMs, runtimeSec, triangleCount, memoryMB, memoryBytes, gcImpactPercent);

        File.AppendAllText(file, line + Environment.NewLine);
        Console.WriteLine($"[n={n} | trial={trial}] {method}: {runtimeMs} ms ({runtimeSec:F3} s) | " +
                          $"triangles={triangleCount} | memory={memoryMB:F2} MB | GC impact ~{gcImpactPercent:F2}%");
    }

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
