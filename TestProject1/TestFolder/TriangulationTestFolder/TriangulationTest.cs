using ClassLibrary2.GeometryFolder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        public void TestDelaunaySinglePointInsert()
        {
            Assert.IsNotNull(triangulator, "Triangulator should not be null.");
            Assert.IsNotNull(vertexArray, "Vertex array should not be null.");

            var vertex0 = vertexArray!.First();
            triangulator!.AddVertices(vertex0);
            triangulator.ProcessSingleVertex();

            // Use the helper
            AssertVertexDelaunay(vertex0);
        }
    }
}
