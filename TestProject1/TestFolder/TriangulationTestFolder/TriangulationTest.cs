using ClassLibrary2.GeometryFolder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TestProject1.TestFolder.TriangulationFolder
{
    [TestClass]
    public class TriangulationTest
    {
        private Vertex[]? vertexArray;
        private TriangulationBuilder? triangulator;
        private Face? superTriangle;

        [TestInitialize]
        public void Setup()
        {
            // Initialize Vertex array from points
            var points = new float[,]
            {
                {560.0f, 191.0f},
                {560.0f, 355.0f},
                {449.0f, 279.0f},
                {688.0f, 277.0f},
                {663.0f, 199.0f},
                {635.0f, 174.0f},
                {470.0f, 344.0f},
                {652.0f, 357.0f},
                {560.0f, 235.0f}
            };

            vertexArray = new Vertex[points.GetLength(0)];
            for (int i = 0; i < points.GetLength(0); i++)
            {
                vertexArray[i] = new Vertex(points[i, 0], points[i, 1]);
            }

            TriangulationOperation.getSuperTriangle(ref vertexArray, out superTriangle!);
            triangulator = new TriangulationBuilder(superTriangle!);
        }

        /// <summary>
        /// Helper method to check that a vertex insertion does not violate Delaunay condition
        /// with respect to its connected edges and their twins.
        /// </summary>
        /// <param name="vertex">The vertex to check</param>
        private void AssertVertexDelaunay(Vertex vertex)
        {
            Assert.IsNotNull(vertex, "Vertex cannot be null.");

            // Get all next edges that have a twin
            var nextEdgesWithTwin = vertex.GetVertexEdges()
                                          .Where(e => e.Next?.Twin != null)
                                          .Select(e => e.Next!);  // select the Next edge

            foreach (var edge in nextEdgesWithTwin)
            {
                var twinEdge = edge.Twin!;
                var twinFace = twinEdge.Face;

                if (GeometryUtils.IsInsideOrOnCircumcircle(twinFace, vertex))
                {
                    Assert.Fail(
                        $"Delaunay violation detected!\n" +
                        $"Inserted Vertex: {vertex}\n" +
                        $"Edge: {edge}\n" +
                        $"Edge.Face: {edge.Face}\n" +
                        $"Twin.Face: {twinFace}"
                    );
                }
            }
        }

        [TestMethod]
        public void TestDelaunay1()
        {
            Assert.IsNotNull(triangulator, "Triangulator should not be null.");
            Assert.IsNotNull(vertexArray, "Vertex array should not be null.");

            foreach (var vertex_current in vertexArray)
            {
                // Add vertex to the triangulator
                triangulator.AddVertices(vertex_current);

                // Get the step-by-step Delaunay operations
                var actions = triangulator.ProcessSingleVertexStepByStep();
                var enumerator = actions.GetEnumerator();
                enumerator.MoveNext();


   
                    do
                    {
                        Debug.WriteLine("------------------------");

                        // --- Pre-action: store previous edge info for validation ---
                        (Vertex origin, Vertex dest)? expected_flipedge = null;

                        foreach (var edge in vertex_current.GetVertexEdges().Reverse())
                        {
                            var e0 = edge.Next?.Twin;
                            if (e0 == null)
                                continue;

                            if (GeometryUtils.IsInsideOrOnCircumcircle(e0.Face, vertex_current))
                            {
                                expected_flipedge = (origin: e0.Origin, dest: e0.Dest);
                                break; // store only the first matching edge
                            }
                        }

                        // --- Iterate edges and perform flips, check for expected edge ---
                        var edges = vertex_current.GetVertexEdges().Reverse().ToList();
                        int count = edges.Count;
                        int matchIndex = -1; // initialize to -1 meaning no match found

                        if (!expected_flipedge.HasValue)
                            continue;

                        if (expected_flipedge.HasValue)
                        {
                            Debug.WriteLine($"Expected flip edge: Origin({expected_flipedge.Value.origin.Position.X}, {expected_flipedge.Value.origin.Position.Y}) " +
                                            $"-> Dest({expected_flipedge.Value.dest.Position.X}, {expected_flipedge.Value.dest.Position.Y})");
                        }

                    } while (enumerator.MoveNext());
                }
        }
    }
}
