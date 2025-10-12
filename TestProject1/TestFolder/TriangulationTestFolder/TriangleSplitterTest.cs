using ClassLibrary2.GeometryFolder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class TriangleSplitterTest
    {
        private readonly TriangleSplitter splitter = new TriangleSplitter();

        // 🔹 Common geometry assertions reused by all tests
        private static void AssertAllFacesConsistent(Vertex vertex)
        {
            var areas = vertex.GetVertexEdges()
                .Select(e => GeometryUtils.GetSignedArea(e.Face.GetVertices().ToArray()))
                .Where(a => Math.Abs(a) > GeometryUtils.GetEpsilon)
                .ToList();

            Assert.IsTrue(areas.Count > 0, "No valid faces found with non-zero area.");

            var firstSign = Math.Sign(areas[0]);
            Assert.IsTrue(areas.All(a => Math.Sign(a) == firstSign),
                "Not all faces around the vertex have the same oriented area sign.");
        }

        // 🔹 Removes expected edges that are replaced around a vertex after splitting
        private static void AssertAllOriginalEdgesRemoved(Vertex vertex, HashSet<HalfEdge> originalEdges)
        {
            vertex.GetVertexEdges()
                .ToList()
                .ForEach(e => originalEdges.Remove(e.Next!));

            Assert.AreEqual(0, originalEdges.Count, "Some original edges remain unprocessed.");
        }

        [TestMethod]
        public void SplitTriangle_PointInsideFace()
        {
            // Arrange
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var face = new Face(vA, vB, vC);

            var vD = new Vertex(0.3f, 0.3f);
            var originalEdges = new HashSet<HalfEdge>(face.GetEdges());

            // Act
            splitter.SplitTriangle(face, vD);

            // Assert
            AssertAllOriginalEdgesRemoved(vD, originalEdges);
            AssertAllFacesConsistent(vD);
        }

        [TestMethod]
        public void SplitTriangle_PointOnEdge()
        {
            // Arrange
            var vA = new Vertex(0.25f, 0f);
            var vB = new Vertex(0.75f, 0f);
            var vC = new Vertex(0.5f, 0.5f);
            var vD = new Vertex(0.5f, -0.5f);

            var face1 = new Face(vA, vB, vC);
            var face2 = new Face(vB, vA, vD);

            // Link twins
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

            var edge = face1.Edge;
            var vE = new Vertex((vA.Position.X + vB.Position.X) / 2f, (vA.Position.Y + vB.Position.Y) / 2f);

            var originalEdges = new HashSet<HalfEdge>
            {
                edge.Next!, edge.Next!.Next!,
                edge.Twin!.Next!, edge.Twin!.Next!.Next!
            };

            // Act
            splitter.SplitTriangle_VertexOnEdge(edge, vE);

            // Assert
            AssertAllOriginalEdgesRemoved(vE, originalEdges);
            AssertAllFacesConsistent(vE);
        }
    }
}
