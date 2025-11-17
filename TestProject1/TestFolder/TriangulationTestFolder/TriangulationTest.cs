using ClassLibrary2.MeshFolder.Else;
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
        private DelaunayBuilder? triangulator;
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

            var superTriangle = TriangulationOperation.GetSuperTriangle(vertexArray);
            triangulator = new DelaunayBuilder(superTriangle!);
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
            var nextEdgesWithTwin = vertex.GetEdges()
                                          .Where(e => e.Next?.Twin != null)
                                          .Select(e => e.Next!);  // select the Next edge

            foreach (var edge in nextEdgesWithTwin)
            {
                var twinEdge = edge.Twin!;
                var twinFace = twinEdge.Face;

                if (GeometryUtils.InCircumcircle(twinFace, vertex))
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


        private void VerifyFlipEdge(Vertex vertex, (Vertex origin, Vertex dest) expectedFlipEdge)
        {
            var edgeList = vertex.GetEdges().Reverse().ToList();
            int count = edgeList.Count;
            int verifyIndex = -1;

            for (int i = 0; i < count; i++)
            {
                var nextEdge = edgeList[(i + 1) % count];
                var v1 = nextEdge.Dest;
                var currentEdge = edgeList[i];
                var v2 = currentEdge.Twin?.Next?.Dest;

                if (v1==expectedFlipEdge.origin && v2 == expectedFlipEdge.dest)
                {
                    verifyIndex = i;
                    break;
                }
            }

            Assert.AreNotEqual(
                -1,
                verifyIndex,
                $"Flip verification failed — expected edge ({expectedFlipEdge.origin}, {expectedFlipEdge.dest}) not found."
            );


            for (int j = 0; j < verifyIndex; j++)
            {
                var currentedge = edgeList[j].Next?.Twin;

                // Verify Delaunay condition
                if (GeometryUtils.InCircumcircle(currentedge.Face, vertex))
                {
                    var vertexDesc = vertex?.ToString() ?? "UnknownVertex";

                    Assert.Fail(
                        $"[Delaunay Verification Failed] " +
                        $"Previously processed region violated at edge index {j}: " +
                        $"Vertex {vertexDesc} lies inside circumcircle of {currentedge.Face}."
                    );
                }
            }

            var flippededge = edgeList[verifyIndex];

            TriangulationOperation.FlipEdge(flippededge);


            if (!GeometryUtils.InCircumcircle(flippededge.Face, vertex))
            {
                Assert.Fail(
                    $"[Delaunay Flip Validation Failed] " +
                    $"Edge flip produced a non-Delaunay configuration. " +
                    $"Flipped edge: {flippededge}. " +
                    $"Vertex {vertex} should lie inside the circumcircle of {flippededge.Face}."
                );
            }
            TriangulationOperation.FlipEdge(flippededge);


        }


        [TestMethod]
        private void ProcessVertexFlipEdges(Vertex vertex)
        {
            var enumerator = triangulator!.ProcessSingleVertexStepByStep().GetEnumerator();
            (Vertex origin, Vertex dest)? expectedFlipEdge = null;

            while (true)
            {
                bool hasNext = enumerator.MoveNext();
                if (!hasNext) break;

                // Perform verification against previous expected edge
                if (expectedFlipEdge.HasValue)
                    VerifyFlipEdge(vertex, expectedFlipEdge.Value);

                // Current edge being flipped
                HalfEdge? currentEdge = enumerator.Current;

                // Update expected flip edge for validation
                if (currentEdge != null && GeometryUtils.InCircumcircle(currentEdge.Face, vertex))
                {
                    expectedFlipEdge = (currentEdge.Origin, currentEdge.Dest!);
                }
                else
                {
                    continue;
                }
            }
        }


        [TestMethod]
        public void TestDelaunay1()
        {
            Assert.IsNotNull(triangulator, "Triangulator should not be null.");
            Assert.IsNotNull(vertexArray, "Vertex array should not be null.");

            foreach (var vertex in vertexArray)
            {
                triangulator.AddVertices(vertex);
                ProcessVertexFlipEdges(vertex);
                AssertVertexDelaunay(vertex);
            }


        }
    }
}